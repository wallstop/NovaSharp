# Packaging Scripts

Scripts for building and distributing NovaSharp packages.

## Scripts

### `build-unity-package.sh` / `build-unity-package.ps1`

Builds a Unity Package Manager (UPM) compatible package from the NovaSharp assemblies.

**Usage (Bash):**
```bash
./scripts/packaging/build-unity-package.sh --version 3.0.0 --output artifacts/unity
```

**Usage (PowerShell):**
```powershell
./scripts/packaging/build-unity-package.ps1 -Version "3.0.0" -OutputPath "artifacts/unity"
```

**Options:**
| Option | Description | Default |
|--------|-------------|---------|
| `--version` / `-Version` | Package version | `3.0.0` |
| `--output` / `-OutputPath` | Output directory | `artifacts/unity` |
| `--configuration` / `-Configuration` | Build configuration | `Release` |

**Output Structure:**
```
artifacts/unity/com.wallstop-studios.novasharp/
├── package.json                    # UPM manifest
├── LICENSE.md
├── CHANGELOG.md
├── Runtime/
│   ├── WallstopStudios.NovaSharp.Interpreter.dll
│   ├── WallstopStudios.NovaSharp.Interpreter.Infrastructure.dll
│   ├── WallstopStudios.NovaSharp.Runtime.asmdef
│   ├── CommunityToolkit.HighPerformance.dll
│   ├── Microsoft.Extensions.ObjectPool.dll
│   ├── System.Text.Json.dll
│   ├── ZString.dll
│   └── Debuggers/
│       ├── WallstopStudios.NovaSharp.RemoteDebugger.dll
│       ├── WallstopStudios.NovaSharp.VsCodeDebugger.dll
│       └── WallstopStudios.NovaSharp.Debuggers.asmdef
├── Documentation~/
│   ├── README.md
│   ├── UnityIntegration.md
│   └── ThirdPartyLicenses.md
└── Samples~/
    └── BasicUsage/
        └── BasicUsage.cs
```

## NuGet Publishing

NuGet packages are published via the `.github/workflows/nuget-publish.yml` workflow.

### Automatic Publishing (Releases)

When a GitHub Release is published:
1. The workflow extracts the version from the release tag (e.g., `v3.0.0` → `3.0.0`)
2. Builds and tests the solution
3. Packs NuGet packages with SourceLink enabled
4. Pushes to NuGet.org (requires `NUGET_API_KEY` secret)
5. Pushes to GitHub Packages

### Manual Publishing

1. Go to Actions → "NuGet Publish" workflow
2. Click "Run workflow"
3. Enter the version (e.g., `3.0.0`, `3.0.1-preview.1`)
4. Optionally enable "Dry run" to build without publishing

### Published Packages

| Package | Description |
|---------|-------------|
| `WallstopStudios.NovaSharp.Interpreter` | Core Lua interpreter |
| `WallstopStudios.NovaSharp.Interpreter.Infrastructure` | Shared infrastructure |
| `WallstopStudios.NovaSharp.VsCodeDebugger` | VS Code Debug Adapter Protocol implementation |
| `WallstopStudios.NovaSharp.RemoteDebugger` | Web-based remote debugger |

### Local Pack

To build NuGet packages locally:

```bash
# Build solution
dotnet build src/NovaSharp.sln -c Release

# Pack all publishable projects
dotnet pack src/runtime/WallstopStudios.NovaSharp.Interpreter/WallstopStudios.NovaSharp.Interpreter.csproj \
    -c Release -o artifacts/packages

dotnet pack src/runtime/WallstopStudios.NovaSharp.Interpreter.Infrastructure/WallstopStudios.NovaSharp.Interpreter.Infrastructure.csproj \
    -c Release -o artifacts/packages

dotnet pack src/debuggers/WallstopStudios.NovaSharp.VsCodeDebugger/WallstopStudios.NovaSharp.VsCodeDebugger.csproj \
    -c Release -o artifacts/packages

dotnet pack src/debuggers/WallstopStudios.NovaSharp.RemoteDebugger/WallstopStudios.NovaSharp.RemoteDebugger.csproj \
    -c Release -o artifacts/packages
```

## Configuration

### NuGet Metadata

Shared NuGet metadata is defined in `Directory.Build.props`:
- Authors, Company, Product
- License (MIT)
- Repository URLs
- Package tags
- SourceLink configuration
- Symbol package generation

Project-specific metadata (Description, PackageId) is in each `.csproj` file.

### Secrets Required

| Secret | Purpose |
|--------|---------|
| `NUGET_API_KEY` | API key for publishing to NuGet.org |
| `GITHUB_TOKEN` | Automatically provided for GitHub Packages |

## IL2CPP/AOT Considerations

When using NovaSharp in IL2CPP builds:

1. **Reflection**: NovaSharp uses reflection for CLR interop. Use `[Preserve]` attributes or link.xml to prevent stripping.

2. **AOT Compilation**: Pre-register types that will be used with Lua:
   ```csharp
   UserData.RegisterType<MyGameClass>();
   ```

3. **Sandboxing**: The sandbox features (instruction limits, memory tracking) work in IL2CPP builds.

See `docs/UnityIntegration.md` for detailed Unity integration guidance.
