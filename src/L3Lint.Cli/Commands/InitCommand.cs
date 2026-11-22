using System.CommandLine;
using System.Globalization;
using System.Text;
using L3Lint.Core;
using L3Lint.Hosting;

namespace L3Lint.Cli.Commands;

/// <summary>
/// <c>l3lint init</c>: writes <c>.l3lint.yaml</c>, <c>.l3lint.lock</c>, a sample payload and a sample workflow
/// (US-07, FR-032, ADR-0041).
/// </summary>
/// <remarks>Existing files are never overwritten without <c>--force</c>; when any target exists, nothing is written.</remarks>
internal static class InitCommand
{
    private const string SamplePath = "samples/l3lint-sample.json";
    private const string WorkflowPath = ".github/workflows/l3lint.yml";

    public static Command Create(CommandContext context)
    {
        var directory = new Option<string>("--directory", "-d") { Description = Resources.Strings.OptDirectory, HelpName = "path", DefaultValueFactory = _ => "." };
        var force = new Option<bool>("--force") { Description = Resources.Strings.OptForce };
        var command = new Command("init", Resources.Strings.CmdInit) { directory, force };
        command.SetAction((result, cancellationToken) => context.GuardAsync(result, async () =>
        {
            var environment = context.Environment;
            var root = Path.GetFullPath(Path.Combine(environment.WorkingDirectory, result.GetValue(directory) ?? "."));
            var linter = Linters.Create(context, result, []);
            var files = new (string Path, string Content)[]
            {
                (L3LintConfiguration.FileName, Template("l3lint.yaml")),
                (LockFile.FileName, LockFile.Render(linter.RulePacks)),
                (SamplePath, Template("sample.json")),
                (WorkflowPath, Template("workflow.yml").Replace("{{version}}", EngineInfo.Version, StringComparison.Ordinal)),
            };

            var existing = files.Where(f => File.Exists(Path.Combine(root, f.Path))).Select(f => f.Path).ToList();
            if (existing.Count > 0 && !result.GetValue(force))
            {
                throw new UsageException(string.Format(CultureInfo.InvariantCulture, Resources.Strings.ErrorInitExists, string.Join(", ", existing)));
            }

            foreach (var (path, content) in files)
            {
                var full = Path.Combine(root, path);
                Directory.CreateDirectory(Path.GetDirectoryName(full)!);
                await File.WriteAllTextAsync(full, content, new UTF8Encoding(false), cancellationToken).ConfigureAwait(false);
                if (!result.GetValue(context.Global.Quiet))
                {
                    environment.Out.WriteLine(string.Format(CultureInfo.InvariantCulture, Resources.Strings.InitWrote, path.Replace('\\', '/')));
                }
            }

            if (!result.GetValue(context.Global.Quiet))
            {
                environment.Out.WriteLine(string.Format(CultureInfo.InvariantCulture, Resources.Strings.InitNext, SamplePath));
            }

            return ExitCodes.Clean;
        }));
        return command;
    }

    private static string Template(string name)
    {
        using var stream = typeof(InitCommand).Assembly.GetManifestResourceStream("L3Lint.Cli.Templates." + name)
            ?? throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, Resources.Strings.ErrorTemplateMissing, name));
        using var reader = new StreamReader(stream, detectEncodingFromByteOrderMarks: true);
        return reader.ReadToEnd().ReplaceLineEndings("\n");
    }
}
