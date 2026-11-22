using System.Collections.Frozen;
using System.Globalization;
using L3Lint.Core.Adapters;
using L3Lint.Core.Detectors;
using L3Lint.Core.Fields;
using L3Lint.Core.Input;
using L3Lint.Core.Model;
using L3Lint.Core.Pci;
using L3Lint.Core.Profiles;
using L3Lint.Core.Rules;
using L3Lint.Core.Schemas;
using L3Lint.Core.Tiers;
using Microsoft.Extensions.Logging;

namespace L3Lint.Core.Engine;

/// <summary>
/// Runs pipeline stages 2 to 9 for one document or transaction. Immutable after construction and safe to call
/// from many threads at once; all per-call state lives on the stack and in per-transaction objects.
/// </summary>
internal sealed partial class LintEngine
{
    private static readonly CardBrand[] AllBrands = [CardBrand.Visa, CardBrand.Mastercard, CardBrand.Amex, CardBrand.Discover];

    private readonly IReadOnlyList<IRule> _rules;
    private readonly FrozenDictionary<string, CompiledRule> _compiled;
    private readonly IReadOnlyList<CompiledRule> _batchRules;
    private readonly FrozenDictionary<CardBrand, ITierPolicy> _policies;
    private readonly TimeProvider _time;
    private readonly ILogger _logger;

    public LintEngine(
        IReadOnlyList<CompiledRule> packRules,
        IReadOnlyList<IRule> customRules,
        IReadOnlyList<ITierPolicy> policies,
        ListLookup lists,
        AdapterRegistry adapters,
        IReadOnlyList<RulePackInfo> packs,
        LintOptions defaults,
        TimeProvider time,
        ILogger logger)
    {
        _rules = [.. packRules.Where(r => r.Batch is null).Cast<IRule>().Concat(customRules).OrderBy(r => r.Metadata.Id, StringComparer.Ordinal)];
        _compiled = packRules.ToFrozenDictionary(r => r.Metadata.Id, StringComparer.Ordinal);
        _batchRules = [.. packRules.Where(r => r.Batch is not null).OrderBy(r => r.Metadata.Id, StringComparer.Ordinal)];
        _policies = policies.ToFrozenDictionary(p => p.Brand);
        Lists = lists;
        Adapters = adapters;
        Packs = packs;
        Defaults = defaults;
        _time = time;
        _logger = logger;
        PackVersions = new SortedDictionary<string, string>(packs.ToDictionary(p => p.Id, p => p.Version), StringComparer.Ordinal);
        AllMetadata = [.. packRules.Select(r => r.Metadata).Concat(customRules.Select(r => r.Metadata)).OrderBy(m => m.Id, StringComparer.Ordinal)];
    }

    public ListLookup Lists { get; }

    public AdapterRegistry Adapters { get; }

    public IReadOnlyList<RulePackInfo> Packs { get; }

    public IReadOnlyDictionary<string, string> PackVersions { get; }

    public LintOptions Defaults { get; }

    public IReadOnlyList<RuleMetadata> AllMetadata { get; }

    public RunContext CreateRun(LintOptions options)
    {
        ProcessorProfile profile;
        if (options.ProcessorProfile is { } custom)
        {
            profile = custom;
        }
        else if (options.Processor is null)
        {
            profile = ProcessorProfiles.Generic;
        }
        else if (!ProcessorProfiles.TryGet(options.Processor, out profile))
        {
            throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, Resources.Strings.ProfileUnknown, options.Processor, string.Join(", ", ProcessorProfiles.All.Select(p => p.Id))), nameof(options));
        }

        var placeholders = Lists.Values("placeholders").Select(p => TextNormalizer.Normalize(p)!)
            .Concat(options.PlaceholdersAdd.Select(p => TextNormalizer.Normalize(p)!))
            .Except(options.PlaceholdersRemove.Select(p => TextNormalizer.Normalize(p)!), StringComparer.Ordinal)
            .Where(p => p.Length > 0)
            .ToFrozenSet(StringComparer.Ordinal);

        return new RunContext
        {
            Options = options,
            Profile = profile,
            Lists = Lists,
            Placeholders = placeholders,
            Selection = new RuleSelection(options, profile),
            PciAllowFields = [.. options.PciAllowFields.Select(p => new PointerPattern(p))],
            Today = DateOnly.FromDateTime(_time.GetUtcNow().UtcDateTime),
        };
    }

    public BatchAggregator CreateBatch(LintOptions options) => new(_batchRules, options);

    /// <summary>Stages 2 to 9 for one document.</summary>
    public LintResult LintDocument(RawDocument document, RunContext run, int firstIndex, int input, BatchAggregator? batch)
    {
        var options = run.Options;
        var adapter = Adapters.Resolve(options.Format, document);

        var scrubbed = document.Kind == InputKind.Json
            ? CardNumberScrubber.ScrubJson(document, adapter, run.PciAllowFields)
            : new ScrubResult(document, []);

        if (options.StrictPci && scrubbed.Matches.Any(m => !m.IsCodeField))
        {
            return StoppedForCardNumber(scrubbed, run, firstIndex, input, adapter.Name);
        }

        var adapterOptions = options.Adapter with { Limits = options.Limits, PciAllowFields = options.PciAllowFields };
        var mapped = adapter.Map(scrubbed.Document, adapterOptions);
        var transactions = new List<TransactionResult>(mapped.Count);
        var errors = new List<InputError>();
        var local = 0;
        foreach (var result in mapped)
        {
            if (result.Transaction.LineItems.Count > options.Limits.MaxLineItems)
            {
                errors.Add(new InputError(
                    document.SourceName,
                    InputErrorKind.TooManyLineItems,
                    string.Format(CultureInfo.InvariantCulture, Resources.Strings.InputTooManyLines, result.Transaction.LineItems.Count, options.Limits.MaxLineItems),
                    result.FirstLine ?? document.FirstLine));
                continue;
            }

            var pci = document.Kind == InputKind.Json ? scrubbed.Matches : result.CardNumberMatches;
            if (options.StrictPci && pci.Any(m => !m.IsCodeField))
            {
                return StoppedForCardNumber(scrubbed with { Matches = pci }, run, firstIndex, input, adapter.Name);
            }

            var issues = SchemaIssues(adapter, scrubbed.Document, result);
            var canonicalTarget = document.Kind == InputKind.Csv;
            var context = new DocumentContext
            {
                Document = scrubbed.Document,
                PatchRoot = canonicalTarget ? CanonicalJson.SerializeToElement(result.Transaction) : scrubbed.Document.Json,
                TargetsCanonical = canonicalTarget,
                FirstLine = result.FirstLine ?? document.FirstLine,
                Format = adapter.Name,
                Input = input,
                OrderBase = firstIndex,
            };
            transactions.Add(Evaluate(result, pci, issues, run, firstIndex + local, local, context, batch, adapterNotes: result.Notes));
            local++;
        }

        return Assemble(transactions, run, document.Sha256, document.SourceName, adapter.Name, errors, stopped: false);
    }

    /// <summary>Stages 2 to 4 only: detect, scrub and map, for convert and map_payload.</summary>
    public MapResult MapDocument(RawDocument document, RunContext run)
    {
        var options = run.Options;
        var adapter = Adapters.Resolve(options.Format, document);
        var scrubbed = document.Kind == InputKind.Json
            ? CardNumberScrubber.ScrubJson(document, adapter, run.PciAllowFields)
            : new ScrubResult(document, []);
        var mapped = adapter.Map(scrubbed.Document, options.Adapter with { Limits = options.Limits, PciAllowFields = options.PciAllowFields });
        return new MapResult(adapter.Name, mapped, [.. scrubbed.Matches.Concat(mapped.SelectMany(m => m.CardNumberMatches))], scrubbed.Document);
    }

    /// <summary>Stages 5 to 9 for an in-memory canonical transaction.</summary>
    public TransactionResult LintTransaction(Transaction transaction, RunContext run, int index, BatchAggregator? batch)
    {
        var result = new AdapterResult(transaction, PathMap.Identity);
        var element = CanonicalJson.SerializeToElement(transaction);
        var issues = SchemaValidator.For("canonical").Validate(element)
            .Select(e => new SchemaIssue(e.Pointer, null, SchemaMessages.DescribeCanonical(e)))
            .ToList();
        var context = new DocumentContext { PatchRoot = element, Format = "canonical", OrderBase = index };
        return Evaluate(result, [], issues, run, index, 0, context, batch, adapterNotes: []);
    }

    public LintResult Assemble(IReadOnlyList<TransactionResult> transactions, RunContext run, string sha256, string? inputName, string? format, IReadOnlyList<InputError> errors, bool stopped)
    {
        var findings = transactions.SelectMany(t => t.Findings).ToList();
        findings.Sort(FindingOrder.Instance);
        var failOn = stopped ? Severity.Error : run.Options.FailOn;
        return new LintResult
        {
            Transactions = transactions,
            Findings = findings,
            Summary = LintSummary.From(findings, transactions.Count(t => t.Kind == TransactionResultKind.Transaction), failOn),
            EngineVersion = EngineInfo.Version,
            RulePackVersions = PackVersions,
            InputSha256 = sha256,
            InputName = inputName,
            Format = format,
            InputErrors = errors,
            StoppedForCardNumber = stopped,
        };
    }

    /// <summary>Builds the trailing batch result from batch rule findings (ADR-0034).</summary>
    public static TransactionResult? CompleteBatch(BatchAggregator batch, RunContext run, int index)
    {
        var drafts = batch.Complete();
        if (drafts.Count == 0)
        {
            return null;
        }

        var state = new TransactionState
        {
            Run = run,
            Transaction = new Transaction(),
            Facts = DerivedFactsBuilder.Build(new Transaction(), run.Options.Arithmetic, new HashSet<string>()),
            AsOf = run.Options.AsOf ?? run.Today,
            Brands = [],
            TransactionIndex = index,
            Paths = PathMap.Identity,
        };
        var context = new DocumentContext { PatchRoot = default };
        var findings = drafts
            .Where(d => run.Selection.Includes(d.Rule.Metadata.Id))
            .Select(d => FindingFactory.Build(d.Draft, state, d.Rule, context))
            .ToList();
        findings.Sort(FindingOrder.Instance);
        return findings.Count == 0 ? null : new TransactionResult
        {
            Kind = TransactionResultKind.Batch,
            Index = index,
            Findings = findings,
            AsOf = state.AsOf,
        };
    }

    private TransactionResult Evaluate(
        AdapterResult mapped,
        IReadOnlyList<CardNumberMatch> pci,
        IReadOnlyList<SchemaIssue> issues,
        RunContext run,
        int index,
        int local,
        DocumentContext document,
        BatchAggregator? batch,
        IReadOnlyList<AdapterNote> adapterNotes)
    {
        var options = run.Options;
        var notes = new List<string>();
        var transaction = ContextOverrides.Apply(mapped.Transaction, options, notes);
        if (options.Adapter.Verbose)
        {
            notes.AddRange(adapterNotes.Where(n => n.Kind != AdapterNoteKind.Invalid).Select(n => n.Message));
        }

        var asOf = options.AsOf ?? transaction.TransactionDate ?? run.Today;
        var brands = EvaluatedBrands(transaction, options);
        var state = new TransactionState
        {
            Run = run,
            Transaction = transaction,
            Facts = DerivedFactsBuilder.Build(transaction, options.Arithmetic, mapped.DerivedFields),
            AsOf = asOf,
            Brands = brands,
            TransactionIndex = index,
            Paths = mapped.Paths,
            CardNumberMatches = pci,
            SchemaIssues = issues,
            InvalidFields = mapped.InvalidFields,
            OrderKey = document.OrderBase + local,
            FirstLine = document.FirstLine,
        };

        var collector = new DraftCollector();
        var generalBrand = brands.Length == 1 ? brands[0] : transaction.Card.Brand ?? CardBrand.Unknown;
        foreach (var rule in _rules)
        {
            var metadata = rule.Metadata;
            if (!Selected(run, metadata, brands, asOf))
            {
                continue;
            }

            var context = new RuleContext(state, metadata.Brand ?? generalBrand);
            try
            {
                if (rule.AppliesTo(context))
                {
                    rule.Evaluate(context, collector);
                }
            }
            catch (Exception ex) when (ex is not OutOfMemoryException)
            {
                // One faulty rule must not hide the others (pipeline stage 8); the run still exits 3.
                LogRuleFailed(_logger, metadata.Id, index, ex);
                collector.Report(new FindingDraft(EngineDiagnostics.RuleFailed, string.Empty)
                {
                    Arguments = new Dictionary<string, string>(StringComparer.Ordinal) { ["detail"] = metadata.Id },
                });
            }
        }

        if (batch is not null)
        {
            foreach (var rule in _batchRules)
            {
                if (!Selected(run, rule.Metadata, brands, asOf))
                {
                    continue;
                }

                var context = new RuleContext(state, generalBrand);
                if (rule.AppliesTo(context))
                {
                    batch.Observe(rule, context);
                }
            }
        }

        if (brands.Length == 0 && run.Selection.Includes(EngineDiagnostics.BrandUnknownId))
        {
            collector.Report(new FindingDraft(EngineDiagnostics.BrandUnknown, "/card/brand"));
        }

        var drafts = FindingFactory.ApplySupersession(collector.Drafts);
        var findings = new List<Finding>(drafts.Count);
        foreach (var draft in drafts)
        {
            findings.Add(FindingFactory.Build(draft, state, _compiled.GetValueOrDefault(draft.Rule.Id), document));
        }

        findings.Sort(FindingOrder.Instance);
        var tiers = new List<TierAssessment>();
        foreach (var brand in brands)
        {
            if (_policies.TryGetValue(brand, out var policy))
            {
                tiers.Add(TierRollup.Assess(policy, new RuleContext(state, brand), findings));
            }
        }

        return new TransactionResult
        {
            Index = index,
            Input = document.Input,
            InputName = document.Document?.SourceName,
            Id = transaction.Id,
            Format = document.Format,
            Brand = transaction.Card.Brand ?? CardBrand.Unknown,
            Brands = brands,
            CardProduct = transaction.Card.Product,
            Mcc = transaction.Merchant.Mcc,
            AsOf = asOf,
            FirstLine = document.FirstLine,
            Findings = findings,
            Tiers = tiers,
            CostEstimate = options.Rates is { } rates ? Estimates.CostEstimator.Estimate(transaction, tiers, rates) : null,
            Notes = notes,
            Transaction = transaction,
        };
    }

    private static bool Selected(RunContext run, RuleMetadata metadata, IReadOnlyList<CardBrand> brands, DateOnly asOf) =>
        run.Selection.Includes(metadata.Id)
        && (metadata.Brand is not { } brand || brands.Contains(brand))
        && metadata.IsEffective(asOf);

    private static CardBrand[] EvaluatedBrands(Transaction transaction, LintOptions options)
    {
        if (options.AllBrands)
        {
            return AllBrands;
        }

        return transaction.Card.Brand is { } brand && brand != CardBrand.Unknown ? [brand] : [];
    }

    private static List<SchemaIssue> SchemaIssues(IPayloadAdapter adapter, RawDocument document, AdapterResult result)
    {
        var issues = new List<SchemaIssue>();
        if (adapter is CanonicalAdapter)
        {
            issues.AddRange(SchemaValidator.For("canonical").Validate(document.Json)
                .Select(e => new SchemaIssue(e.Pointer, null, SchemaMessages.DescribeCanonical(e))));
            return issues;
        }

        issues.AddRange(SchemaValidator.For("canonical").Validate(CanonicalJson.SerializeToElement(result.Transaction))
            .Select(e => new SchemaIssue(e.Pointer, null, SchemaMessages.DescribeCanonical(e))));
        issues.AddRange(result.Notes.Where(n => n.Kind == AdapterNoteKind.Invalid)
            .Select(n => new SchemaIssue(n.CanonicalPointer ?? string.Empty, n.SourcePointer, n.Message)));
        return issues;
    }

    private LintResult StoppedForCardNumber(ScrubResult scrubbed, RunContext run, int index, int input, string format)
    {
        var rule = _compiled.GetValueOrDefault("GEN-PCI-001");
        var findings = new List<Finding>();
        if (rule is not null)
        {
            var state = new TransactionState
            {
                Run = run,
                Transaction = new Transaction(),
                Facts = DerivedFactsBuilder.Build(new Transaction(), run.Options.Arithmetic, new HashSet<string>()),
                AsOf = run.Options.AsOf ?? run.Today,
                Brands = [],
                TransactionIndex = index,
                Paths = PathMap.Identity,
            };
            var context = new DocumentContext { Document = scrubbed.Document, PatchRoot = default, Format = format, Input = input };
            foreach (var match in scrubbed.Matches)
            {
                findings.Add(FindingFactory.Build(
                    new FindingDraft(rule.Metadata, match.CanonicalPointer ?? string.Empty)
                    {
                        Actual = match.MaskedValue,
                        SourcePointer = match.SourcePointer,
                        Location = match.Location,
                        Severity = match.IsCodeField ? Severity.Warning : null,
                        Arguments = new Dictionary<string, string>(StringComparer.Ordinal) { ["masked"] = match.MaskedValue, ["field"] = match.SourcePointer },
                    },
                    state,
                    rule,
                    context));
            }
        }

        findings.Sort(FindingOrder.Instance);
        var transaction = new TransactionResult
        {
            Index = index,
            Input = input,
            InputName = scrubbed.Document.SourceName,
            Format = format,
            Findings = findings,
            AsOf = run.Options.AsOf ?? run.Today,
            Notes = [Resources.Strings.NoteStrictPciStopped],
        };
        return Assemble([transaction], run, scrubbed.Document.Sha256, scrubbed.Document.SourceName, format, [], stopped: true);
    }

    [LoggerMessage(EventId = 2001, Level = LogLevel.Error, Message = "Rule {RuleId} failed on transaction {TransactionIndex}; other rules still ran.")]
    private static partial void LogRuleFailed(ILogger logger, string ruleId, int transactionIndex, Exception exception);
}
