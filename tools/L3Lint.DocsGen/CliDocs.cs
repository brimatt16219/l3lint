using System.Collections;
using System.CommandLine;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using L3Lint.Cli;
using L3Lint.Cli.Commands;
using L3Lint.Core;
using L3Lint.Core.Profiles;

namespace L3Lint.DocsGen;

/// <summary>
/// Writes docs/reference/cli.md from the real System.CommandLine tree (NFR-014): every command, argument and option
/// with its description, aliases, default and allowed values.
/// </summary>
/// <remarks>
/// The tree is built the way <see cref="CliApplication.RunAsync"/> builds it. To catch drift between the two, the
/// generator also runs <c>--help</c> through <see cref="CliApplication.RunAsync"/> for every command and reports a
/// problem when the commands or options printed there differ from the tree documented here.
/// </remarks>
internal static partial class CliDocs
{
    private const string Page = "docs/reference/cli.md";

    public static void Generate(GeneratedFiles files)
    {
        var root = BuildTree();
        var commands = Walk(root, []).ToList();
        foreach (var (path, command) in commands)
        {
            Validate(files, path, command);
            CompareWithHelp(files, path, command, Ancestors(commands, path));
        }

        files.Add(Page, Render(root, commands));
    }

    /// <summary>Builds the command tree as <see cref="CliApplication.RunAsync"/> does, over an environment that does nothing.</summary>
    private static RootCommand BuildTree()
    {
        var global = new GlobalOptions();
        var root = new RootCommand(L3Lint.Cli.Resources.Strings.RootDescription);
        global.AddTo(root);
        var context = new CommandContext(QuietEnvironment(TextWriter.Null), global);
        root.Add(LintCommand.Create(context));
        root.Add(ExplainCommand.Create(context));
        root.Add(FixCommand.Create(context));
        root.Add(ConvertCommand.Create(context));
        root.Add(RulesCommand.Create(context));
        root.Add(SchemaCommand.Create(context));
        root.Add(InitCommand.Create(context));
        root.Add(VersionCommand.Create(context));
        return root;
    }

    private static CliEnvironment QuietEnvironment(TextWriter output) => new()
    {
        Out = output,
        Error = TextWriter.Null,
        OpenStandardInput = () => Stream.Null,
        WorkingDirectory = Directory.GetCurrentDirectory(),
        GetVariable = _ => null,
    };

    private static IEnumerable<(string[] Path, Command Command)> Walk(Command command, string[] path)
    {
        yield return (path, command);
        foreach (var sub in command.Subcommands.Where(c => !c.Hidden))
        {
            foreach (var item in Walk(sub, [.. path, sub.Name]))
            {
                yield return item;
            }
        }
    }

    private static List<Command> Ancestors(List<(string[] Path, Command Command)> commands, string[] path) =>
        [.. Enumerable.Range(0, path.Length).Select(n => commands.Single(c => c.Path.AsSpan().SequenceEqual(path.AsSpan(0, n))).Command)];

    private static void Validate(GeneratedFiles files, string[] path, Command command)
    {
        var name = Name(path);
        if (string.IsNullOrWhiteSpace(command.Description))
        {
            files.Problem($"command {name} has no description");
        }

        foreach (var argument in command.Arguments.Where(a => !a.Hidden && string.IsNullOrWhiteSpace(a.Description)))
        {
            files.Problem($"argument {argument.Name} of {name} has no description");
        }

        foreach (var option in command.Options.Where(o => !o.Hidden && string.IsNullOrWhiteSpace(o.Description)))
        {
            files.Problem($"option {option.Name} of {name} has no description");
        }
    }

    /// <summary>Compares the options and subcommands the tool prints for --help with the documented tree.</summary>
    private static void CompareWithHelp(GeneratedFiles files, string[] path, Command command, List<Command> ancestors)
    {
        using var output = new StringWriter(CultureInfo.InvariantCulture);
        var code = CliApplication.RunAsync([.. path, "--help"], QuietEnvironment(output)).GetAwaiter().GetResult();
        if (code != 0)
        {
            files.Problem($"{Name(path)} --help exited with {code.ToString(CultureInfo.InvariantCulture)}");
            return;
        }

        var help = Sections(output.ToString());
        var printedOptions = help.TryGetValue("Options:", out var optionLines)
            ? optionLines.SelectMany(l => OptionToken().Matches(l.Split("  ", 2, StringSplitOptions.TrimEntries)[0]).Select(m => m.Value)).ToHashSet(StringComparer.Ordinal)
            : [];
        var documentedOptions = command.Options.Where(o => !o.Hidden)
            .Concat(ancestors.SelectMany(a => a.Options).Where(o => o.Recursive && !o.Hidden))
            .SelectMany(o => new[] { o.Name }.Concat(o.Aliases))
            .Where(n => n.StartsWith('-')) // help omits the Windows-style aliases /h and /?
            .ToHashSet(StringComparer.Ordinal);
        if (!printedOptions.SetEquals(documentedOptions))
        {
            files.Problem($"{Name(path)}: --help prints options [{string.Join(", ", printedOptions.Order(StringComparer.Ordinal))}] but the documented tree has [{string.Join(", ", documentedOptions.Order(StringComparer.Ordinal))}]");
        }

        var printedCommands = help.TryGetValue("Commands:", out var commandLines)
            ? commandLines.Select(l => l.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0]).ToHashSet(StringComparer.Ordinal)
            : [];
        var documentedCommands = command.Subcommands.Where(c => !c.Hidden).Select(c => c.Name).ToHashSet(StringComparer.Ordinal);
        if (!printedCommands.SetEquals(documentedCommands))
        {
            files.Problem($"{Name(path)}: --help prints commands [{string.Join(", ", printedCommands.Order(StringComparer.Ordinal))}] but the documented tree has [{string.Join(", ", documentedCommands.Order(StringComparer.Ordinal))}]");
        }
    }

    /// <summary>Splits help text into its sections ("Options:", "Commands:"), keeping the indented entry lines.</summary>
    private static Dictionary<string, List<string>> Sections(string help)
    {
        var sections = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        List<string>? current = null;
        foreach (var line in help.ReplaceLineEndings("\n").Split('\n'))
        {
            if (line.Length > 0 && !char.IsWhiteSpace(line[0]))
            {
                current = sections[line.Trim()] = [];
            }
            else if (current is not null && line.StartsWith("  ", StringComparison.Ordinal) && line.Length > 2 && !char.IsWhiteSpace(line[2]))
            {
                current.Add(line[2..]);
            }
        }

        return sections;
    }

    private static string Render(RootCommand root, List<(string[] Path, Command Command)> commands)
    {
        var page = new StringBuilder();
        page.Append("# CLI reference\n\n");
        page.Append("<!-- Generated by tools/L3Lint.DocsGen from the l3lint command tree. Do not edit by hand. -->\n\n");
        page.Append(CultureInfo.InvariantCulture, $"This page lists every command, argument and option of `l3lint` {EngineInfo.Version}. ");
        page.Append("It is generated from the same command tree the tool parses, so it matches `l3lint <command> --help`. ");
        page.Append("For a task-oriented guide with examples, see [Using the CLI](../articles/cli.md).\n\n");

        page.Append("## Commands\n\n| Command | Description |\n| --- | --- |\n");
        foreach (var (path, command) in commands.Where(c => c.Path.Length > 0))
        {
            page.Append("| [").Append(Markdown.Code(Name(path))).Append("](#").Append(Anchor(path)).Append(") | ")
                .Append(Markdown.Cell(command.Description)).Append(" |\n");
        }

        page.Append("\n## Global options\n\n");
        page.Append("These options belong to the root command. Options marked \"every command\" also apply after any command name, ");
        page.Append("for example `l3lint lint payload.json --no-color`.\n\n");
        OptionTable(page, root.Options, showScope: true);

        foreach (var (path, command) in commands.Where(c => c.Path.Length > 0))
        {
            page.Append("<a id=\"").Append(Anchor(path)).Append("\"></a>\n\n");
            page.Append("## ").Append(Name(path)).Append("\n\n");
            page.Append(Markdown.Cell(command.Description)).Append("\n\n");
            page.Append("Usage: ").Append(Markdown.Code(Usage(path, command))).Append("\n\n");
            if (command.Arguments.Any(a => !a.Hidden))
            {
                page.Append("### Arguments\n\n| Argument | Count | Description |\n| --- | --- | --- |\n");
                foreach (var argument in command.Arguments.Where(a => !a.Hidden))
                {
                    page.Append("| ").Append(Markdown.Code("<" + argument.Name + ">")).Append(" | ")
                        .Append(Count(argument.Arity)).Append(" | ")
                        .Append(Markdown.Cell(argument.Description)).Append(" |\n");
                }

                page.Append('\n');
            }

            var subcommands = command.Subcommands.Where(c => !c.Hidden).ToList();
            if (subcommands.Count > 0)
            {
                page.Append("### Subcommands\n\n| Command | Description |\n| --- | --- |\n");
                foreach (var sub in subcommands)
                {
                    string[] subPath = [.. path, sub.Name];
                    page.Append("| [").Append(Markdown.Code(Name(subPath))).Append("](#").Append(Anchor(subPath)).Append(") | ")
                        .Append(Markdown.Cell(sub.Description)).Append(" |\n");
                }

                page.Append('\n');
            }

            var options = command.Options.Where(o => !o.Hidden).ToList();
            if (options.Count > 0)
            {
                page.Append("### Options\n\n");
                OptionTable(page, options, showScope: false);
            }

            page.Append("The [global options](#global-options) marked \"every command\" also apply.\n\n");
        }

        return page.ToString().TrimEnd('\n') + "\n";
    }

    private static void OptionTable(StringBuilder page, IEnumerable<Option> options, bool showScope)
    {
        page.Append(showScope
            ? "| Option | Aliases | Value | Default | Applies to | Description |\n| --- | --- | --- | --- | --- | --- |\n"
            : "| Option | Aliases | Value | Default | Description |\n| --- | --- | --- | --- | --- |\n");
        foreach (var option in options.Where(o => !o.Hidden))
        {
            page.Append("| ").Append(Markdown.Code(option.Name)).Append(" | ")
                .Append(option.Aliases.Count == 0 ? "none" : string.Join(", ", option.Aliases.Order(StringComparer.Ordinal).Select(Markdown.Code))).Append(" | ")
                .Append(Value(option)).Append(" | ")
                .Append(Default(option)).Append(" | ");
            if (showScope)
            {
                page.Append(option.Recursive ? "every command" : "`l3lint` only").Append(" | ");
            }

            page.Append(Markdown.Cell(option.Description)).Append(" |\n");
        }

        page.Append('\n');
    }

    /// <summary>
    /// Describes the value an option takes: allowed values, a placeholder, or none for a flag. A help name such as
    /// <c>auto|canonical|stripe</c> lists the allowed values; the two help names below are placeholders for kinds of
    /// value instead, so they are described in words (the profile names come from the embedded profiles).
    /// </summary>
    private static string Value(Option option)
    {
        if (IsFlag(option))
        {
            return "none (flag)";
        }

        var text = option.HelpName switch
        {
            "profile|path" => "a profile name (" + string.Join(", ", ProcessorProfiles.All.Select(p => Markdown.Code(p.Id))) + ") or the path of a profile YAML file",
            "rule|glob" => "a rule ID, or a glob such as `VISA-DQ-*`",
            { } help when help.Contains('|', StringComparison.Ordinal) => "one of " + string.Join(", ", help.Split('|').Select(Markdown.Code)),
            var help => Markdown.Code("<" + (help ?? option.Name.TrimStart('-')) + ">"),
        };
        if (typeof(IEnumerable).IsAssignableFrom(option.ValueType) && option.ValueType != typeof(string))
        {
            text += "; repeatable";
        }

        if (option.Required)
        {
            text += "; required";
        }

        return text;
    }

    /// <summary>The default from the option's own default value, else from the "Default:" sentence of its description.</summary>
    private static string Default(Option option)
    {
        if (IsFlag(option))
        {
            return "off";
        }

        if (option.HasDefaultValue && option.GetDefaultValue() is { } value && Convert.ToString(value, CultureInfo.InvariantCulture) is { Length: > 0 } text)
        {
            return Markdown.Code(text);
        }

        if (option.Description is { } description && DefaultSentence().Match(description) is { Success: true } match)
        {
            return Markdown.Cell(match.Groups[1].Value);
        }

        return "none";
    }

    /// <summary>An option that takes no value, such as <c>--quiet</c>, <c>--help</c> or <c>--version</c>.</summary>
    private static bool IsFlag(Option option) => option.ValueType == typeof(bool) || option.Arity.MaximumNumberOfValues == 0;

    private static string Count(ArgumentArity arity) => (arity.MinimumNumberOfValues, arity.MaximumNumberOfValues) switch
    {
        (1, 1) => "exactly one",
        (0, 1) => "optional",
        (1, _) => "one or more",
        (0, _) => "any number",
        var (min, max) => $"{min.ToString(CultureInfo.InvariantCulture)} to {max.ToString(CultureInfo.InvariantCulture)}",
    };

    private static string Usage(string[] path, Command command)
    {
        var usage = new StringBuilder(Name(path));
        if (command.Subcommands.Any(c => !c.Hidden))
        {
            usage.Append(" <command>");
        }

        foreach (var argument in command.Arguments.Where(a => !a.Hidden))
        {
            usage.Append(" <").Append(argument.Name).Append('>');
            if (argument.Arity.MaximumNumberOfValues > 1)
            {
                usage.Append("...");
            }
        }

        if (command.Options.Any(o => !o.Hidden))
        {
            usage.Append(" [options]");
        }

        return usage.ToString();
    }

    private static string Name(string[] path) => path.Length == 0 ? "l3lint" : "l3lint " + string.Join(" ", path);

    private static string Anchor(string[] path) => "command-" + string.Join("-", path);

    [GeneratedRegex(@"(?<=^|[ ,])--?[A-Za-z?][A-Za-z0-9-]*", RegexOptions.CultureInvariant)]
    private static partial Regex OptionToken();

    [GeneratedRegex(@"Default: (.+?)\.(?:\s|$)", RegexOptions.CultureInvariant)]
    private static partial Regex DefaultSentence();
}
