using L3Lint.Cli;

// The l3lint command-line tool. Results go to stdout; logs, notes and errors go to stderr (NFR-012).
return await CliApplication.RunAsync(args, CliEnvironment.FromConsole()).ConfigureAwait(false);
