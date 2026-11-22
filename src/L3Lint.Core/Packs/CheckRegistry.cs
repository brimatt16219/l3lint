using System.Globalization;
using L3Lint.Core.Rules;

namespace L3Lint.Core.Packs;

/// <summary>Compiled custom and batch checks by name. Names are unique across every registered source.</summary>
internal sealed class CheckRegistry
{
    private readonly Dictionary<string, ICustomCheck> _custom = new(StringComparer.Ordinal);
    private readonly Dictionary<string, IBatchCheck> _batch = new(StringComparer.Ordinal);

    public void Add(ICustomCheck check)
    {
        if (!_custom.TryAdd(check.Name, check) || _batch.ContainsKey(check.Name))
        {
            throw new RulePackException(string.Format(CultureInfo.InvariantCulture, Resources.Strings.PackDuplicateCheck, check.Name));
        }
    }

    public void Add(IBatchCheck check)
    {
        if (!_batch.TryAdd(check.Name, check) || _custom.ContainsKey(check.Name))
        {
            throw new RulePackException(string.Format(CultureInfo.InvariantCulture, Resources.Strings.PackDuplicateCheck, check.Name));
        }
    }

    public bool TryGetCustom(string name, out ICustomCheck check) => _custom.TryGetValue(name, out check!);

    public bool TryGetBatch(string name, out IBatchCheck check) => _batch.TryGetValue(name, out check!);
}
