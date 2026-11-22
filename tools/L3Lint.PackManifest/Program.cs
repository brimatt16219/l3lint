using L3Lint.PackManifest;

// Writes (or with --check verifies) manifest.json for each rule pack folder given on the command line.
// With no folders it processes the five shipped packs under src/.
return ManifestTool.Run(args, Console.Out, Console.Error);
