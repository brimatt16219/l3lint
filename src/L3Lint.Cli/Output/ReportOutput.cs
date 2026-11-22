using System.Globalization;
using System.Text;
using L3Lint.Hosting;
using L3Lint.Reporting;

namespace L3Lint.Cli.Output;

/// <summary>Writes a report to every requested target. A file that cannot be written is an exit-3 error (pipeline stage 10).</summary>
internal static class ReportOutput
{
    private static readonly UTF8Encoding Utf8 = new(encoderShouldEmitUTF8Identifier: false);

    /// <summary>Checks every requested format before any linting or writing, so a bad request never leaves half the outputs behind.</summary>
    public static void EnsureAvailable(IReadOnlyList<OutputTarget> targets)
    {
        foreach (var target in targets)
        {
            if (!ReportWriters.TryGet(target.Format, out _))
            {
                throw new UsageException(string.Format(CultureInfo.InvariantCulture, Resources.Strings.ErrorOutputUnavailable, target.Format));
            }
        }
    }

    public static bool Write(LintReport report, IReadOnlyList<OutputTarget> targets, ReportWriterOptions options, CliEnvironment environment)
    {
        EnsureAvailable(targets);
        foreach (var target in targets)
        {
            ReportWriters.TryGet(target.Format, out var writer);
            if (target.Path is null)
            {
                writer.Write(report, environment.Out, options);
                continue;
            }

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(target.Path)!);
                using var file = new StreamWriter(target.Path, append: false, Utf8) { NewLine = "\n" };
                writer.Write(report, file, options with { Color = false });
            }
            catch (IOException ex)
            {
                environment.Error.WriteLine(Resources.Strings.ErrorPrefix + string.Format(CultureInfo.InvariantCulture, Resources.Strings.ErrorWriteOutput, target.Path, ex.Message));
                return false;
            }
            catch (UnauthorizedAccessException ex)
            {
                environment.Error.WriteLine(Resources.Strings.ErrorPrefix + string.Format(CultureInfo.InvariantCulture, Resources.Strings.ErrorWriteOutput, target.Path, ex.Message));
                return false;
            }
        }

        return true;
    }
}
