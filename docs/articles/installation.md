# Installation

l3lint runs on Windows, macOS and Linux, on x64 and arm64. There are three ways to get the CLI: as a .NET tool, as a single-file binary that needs no .NET, or built from source.

## As a .NET tool

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

```text
dotnet tool install --global l3lint
l3lint version
```

To pin the version for a repository, install it as a local tool instead, so everyone gets the same version from the tool manifest:

```text
dotnet new tool-manifest
dotnet tool install l3lint
dotnet tool run l3lint version
```

Update with `dotnet tool update --global l3lint`. Remove with `dotnet tool uninstall --global l3lint`.

The install commands above need the `l3lint` package on NuGet.org. Until the first release is published, install from a package you build yourself (see [From source](#from-source)):

```text
$ dotnet pack src/L3Lint.Cli -c Release -o pkg
$ dotnet tool install l3lint --tool-path tools-install --add-source pkg --version 1.0.0
You can invoke the tool using the following command: l3lint
Tool 'l3lint' (version '1.0.0') was successfully installed.
$ tools-install/l3lint version
l3lint 1.0.0
  l3lint.rules.amex 1.0.0
  l3lint.rules.discover 1.0.0
  l3lint.rules.general 1.0.0
  l3lint.rules.mastercard 1.0.0
  l3lint.rules.visa 1.0.0
```

## As a single-file binary

The release plan attaches a self-contained, ReadyToRun, single-file binary for each platform to every GitHub release, with SHA-256 checksums. These binaries need no .NET installation.

| Platform | Runtime identifier |
| --- | --- |
| Windows x64 | `win-x64` |
| Windows arm64 | `win-arm64` |
| Linux x64 | `linux-x64` |
| Linux arm64 | `linux-arm64` |
| macOS Intel | `osx-x64` |
| macOS Apple silicon | `osx-arm64` |

Check the download against its checksum before you run it. On Linux, `sha256sum l3lint`; on macOS, `shasum -a 256 l3lint`; on Windows PowerShell, `Get-FileHash l3lint.exe`. Then mark the file executable on Linux and macOS (`chmod +x l3lint`) and put it on your `PATH`.

## From source

You need the .NET 10 SDK. The repository's `global.json` pins the SDK feature band.

```text
git clone https://github.com/brimatt16219/l3lint.git
cd l3lint
dotnet build L3Lint.slnx
dotnet src/L3Lint.Cli/bin/Debug/net10.0/l3lint.dll version
```

To build a single-file binary yourself, publish the CLI project for one runtime identifier from the table above:

```text
$ dotnet publish src/L3Lint.Cli -c Release -r linux-x64 -o publish/linux-x64
$ dotnet publish src/L3Lint.Cli -c Release -r win-x64 -o publish/win-x64
$ publish/win-x64/l3lint.exe version
l3lint 1.0.0
  l3lint.rules.amex 1.0.0
  l3lint.rules.discover 1.0.0
  l3lint.rules.general 1.0.0
  l3lint.rules.mastercard 1.0.0
  l3lint.rules.visa 1.0.0
```

Setting a runtime identifier turns on single-file, self-contained and ReadyToRun publishing in the project file. The output folder holds the binary (`l3lint` or `l3lint.exe`, about 100 MB) and may also hold `.pdb` and `.xml` files, which the binary does not need.

## Other components

| Component | How to get it | Guide |
| --- | --- | --- |
| MCP server | `dnx L3Lint.Mcp --yes` from an MCP client | [MCP server](mcp-server.md) |
| GitHub Action | `uses: brimatt16219/l3lint/action@v1.0.0` in a workflow | [GitHub Action](github-action.md) |
| .NET library | `dotnet add package L3Lint.Hosting` | [.NET library](library.md) |

## Nothing phones home

The CLI, the MCP server and the library make no network calls, send no telemetry and never check for updates, so they run the same way on a machine with no network access. Only installing needs the network: NuGet for the tool and packages, and in the GitHub Action, the .NET setup and the SARIF upload. See [Privacy and security](privacy-and-security.md).
