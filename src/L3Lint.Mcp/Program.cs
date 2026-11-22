using L3Lint.Mcp;

// The l3lint MCP server. stdio by default; --http serves Streamable HTTP on 127.0.0.1 (Interfaces tab).
return await McpApp.RunAsync(args).ConfigureAwait(false);
