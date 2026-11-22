using L3Lint.Fuzz;
using SharpFuzz;

// Usage: L3Lint.Fuzz <target> <mode> [file]
//   target: json, config, csv or rulepack
//   mode:   libfuzzer (driven by libfuzzer-dotnet), afl (driven by afl-fuzz), or repro (run one file once)
// With no arguments the target comes from L3LINT_FUZZ_TARGET and the mode is libfuzzer, because the
// libfuzzer-dotnet driver passes a single argument (the assembly path). See README.md for the full commands.
var target = args.Length > 0 ? args[0] : Environment.GetEnvironmentVariable("L3LINT_FUZZ_TARGET") ?? string.Empty;
var mode = args.Length > 1 ? args[1] : "libfuzzer";
if (!FuzzTargets.Names.Contains(target))
{
    await Console.Error.WriteLineAsync("Usage: L3Lint.Fuzz <" + string.Join("|", FuzzTargets.Names) + "> <libfuzzer|afl|repro> [file], or set L3LINT_FUZZ_TARGET.");
    return 2;
}

switch (mode)
{
    case "libfuzzer":
        Fuzzer.LibFuzzer.Run(input => FuzzTargets.Run(target, input));
        return 0;
    case "afl":
        Fuzzer.OutOfProcess.Run(stream =>
        {
            using var buffer = new MemoryStream();
            stream.CopyTo(buffer);
            FuzzTargets.Run(target, buffer.ToArray());
        });
        return 0;
    case "repro" when args.Length == 3:
        // An undocumented exception is not caught, so the process fails the way the fuzzer saw it.
        var outcome = FuzzTargets.Run(target, await File.ReadAllBytesAsync(args[2]));
        Console.WriteLine(outcome);
        return 0;
    default:
        await Console.Error.WriteLineAsync("Unknown mode " + mode + ". Use libfuzzer, afl or repro <file>.");
        return 2;
}
