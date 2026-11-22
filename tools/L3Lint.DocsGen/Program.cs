using L3Lint.DocsGen;

// Usage: L3Lint.DocsGen rules|cli|schemas|all [--check]
// Writes the generated pages of the docs site (FR-036, NFR-014): one page per rule, the CLI reference and the
// schema references. --check regenerates in memory, writes nothing, and exits 1 when a generated file differs or is
// missing, or when a rule, command, option or schema property has no description.
var check = args.Contains("--check", StringComparer.Ordinal);
var unknown = args.Where(a => a.StartsWith("--", StringComparison.Ordinal) && a != "--check").ToList();
var command = args.FirstOrDefault(a => !a.StartsWith("--", StringComparison.Ordinal)) ?? "all";
if (unknown.Count > 0 || command is not ("rules" or "cli" or "schemas" or "all"))
{
    Console.Error.WriteLine("Usage: L3Lint.DocsGen rules|cli|schemas|all [--check]");
    return 2;
}

var files = new GeneratedFiles(Repository.Root());
if (command is "rules" or "all")
{
    RuleDocs.Generate(files);
}

if (command is "cli" or "all")
{
    CliDocs.Generate(files);
}

if (command is "schemas" or "all")
{
    SchemaDocs.Generate(files);
}

return files.Finish(check, Console.Out, Console.Error);
