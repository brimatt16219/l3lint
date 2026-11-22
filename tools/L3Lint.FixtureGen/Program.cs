using L3Lint.FixtureGen;

// Usage: L3Lint.FixtureGen examples [--check]
//        L3Lint.FixtureGen samples [--check]
//        L3Lint.FixtureGen corpus [--check]
// --check compares generated content with the committed files and exits 1 when any differ.
var check = args.Contains("--check", StringComparer.Ordinal);
var root = Repository.Root();
var command = args.FirstOrDefault(a => !a.StartsWith("--", StringComparison.Ordinal)) ?? "examples";
var writer = new FileSetWriter(root, check, Console.Out, Console.Error);
switch (command)
{
    case "examples":
        ExampleWriter.Write(writer);
        break;
    case "samples":
        SampleWriter.Write(writer);
        break;
    case "corpus":
        L3Lint.FixtureGen.Corpus.CorpusWriter.Write(writer);
        break;
    default:
        Console.Error.WriteLine($"Unknown command {command}. Use examples, samples or corpus.");
        return 2;
}

return writer.Finish();
