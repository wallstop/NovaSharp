#Requires -Version 5.1
<#
.SYNOPSIS
    Build NovaSharp Unity package
.DESCRIPTION
    Builds the NovaSharp assemblies for Unity (netstandard2.1) and creates a
    Unity-compatible package structure with proper meta files and package.json.
.PARAMETER Version
    Package version (default: 3.0.0)
.PARAMETER OutputPath
    Output directory (default: artifacts/unity)
.PARAMETER Configuration
    Build configuration (default: Release)
.EXAMPLE
    ./build-unity-package.ps1 -Version "3.0.1"
.EXAMPLE
    ./build-unity-package.ps1 -Version "3.0.0-preview.1" -OutputPath "./my-output"
#>

[CmdletBinding()]
param(
    [Parameter()]
    [string]$Version = "3.0.0",

    [Parameter()]
    [string]$OutputPath = "",

    [Parameter()]
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = Resolve-Path (Join-Path $ScriptDir "../..")

if ([string]::IsNullOrEmpty($OutputPath)) {
    $OutputPath = Join-Path $RepoRoot "artifacts/unity"
}

Write-Host "🔨 Building NovaSharp Unity package v$Version" -ForegroundColor Cyan
Write-Host "   Output: $OutputPath"
Write-Host "   Configuration: $Configuration"
Write-Host ""

# Create output directories
$PackageRoot = Join-Path $OutputPath "com.wallstop-studios.novasharp"
$RuntimeDir = Join-Path $PackageRoot "Runtime"
$DebuggersDir = Join-Path $RuntimeDir "Debuggers"
$EditorDir = Join-Path $PackageRoot "Editor"
$DocsDir = Join-Path $PackageRoot "Documentation~"
$SamplesDir = Join-Path $PackageRoot "Samples~/BasicUsage"

New-Item -ItemType Directory -Path $RuntimeDir -Force | Out-Null
New-Item -ItemType Directory -Path $DebuggersDir -Force | Out-Null
New-Item -ItemType Directory -Path $EditorDir -Force | Out-Null
New-Item -ItemType Directory -Path $DocsDir -Force | Out-Null
New-Item -ItemType Directory -Path $SamplesDir -Force | Out-Null

# Build and publish assemblies
Write-Host "📦 Building assemblies..." -ForegroundColor Yellow

$InterpreterProject = Join-Path $RepoRoot "src/runtime/WallstopStudios.NovaSharp.Interpreter/WallstopStudios.NovaSharp.Interpreter.csproj"
$RemoteDebuggerProject = Join-Path $RepoRoot "src/debuggers/WallstopStudios.NovaSharp.RemoteDebugger/WallstopStudios.NovaSharp.RemoteDebugger.csproj"
$VsCodeDebuggerProject = Join-Path $RepoRoot "src/debuggers/WallstopStudios.NovaSharp.VsCodeDebugger/WallstopStudios.NovaSharp.VsCodeDebugger.csproj"

& dotnet publish $InterpreterProject -c $Configuration -f netstandard2.1 -o $RuntimeDir -p:Version=$Version /p:DebugType=portable
if ($LASTEXITCODE -ne 0) { throw "Failed to build Interpreter" }

& dotnet publish $RemoteDebuggerProject -c $Configuration -f netstandard2.1 -o $DebuggersDir -p:Version=$Version /p:DebugType=portable
if ($LASTEXITCODE -ne 0) { throw "Failed to build RemoteDebugger" }

& dotnet publish $VsCodeDebuggerProject -c $Configuration -f netstandard2.1 -o $DebuggersDir -p:Version=$Version /p:DebugType=portable
if ($LASTEXITCODE -ne 0) { throw "Failed to build VsCodeDebugger" }

# Clean up unnecessary files
Write-Host "🧹 Cleaning up..." -ForegroundColor Yellow
Get-ChildItem -Path $PackageRoot -Recurse -Include "*.deps.json", "*.runtimeconfig.json" | Remove-Item -Force -ErrorAction SilentlyContinue

# Remove duplicate assemblies from Debuggers folder
$DuplicateFiles = @(
    "WallstopStudios.NovaSharp.Interpreter.dll",
    "WallstopStudios.NovaSharp.Interpreter.pdb",
    "WallstopStudios.NovaSharp.Interpreter.xml",
    "WallstopStudios.NovaSharp.Interpreter.Infrastructure.dll",
    "WallstopStudios.NovaSharp.Interpreter.Infrastructure.pdb",
    "WallstopStudios.NovaSharp.Interpreter.Infrastructure.xml",
    "CommunityToolkit.HighPerformance.dll",
    "Microsoft.Extensions.ObjectPool.dll",
    "System.Text.Json.dll",
    "ZString.dll"
)
foreach ($file in $DuplicateFiles) {
    $filePath = Join-Path $DebuggersDir $file
    if (Test-Path $filePath) {
        Remove-Item $filePath -Force
    }
}

# Copy documentation
Write-Host "📝 Copying documentation..." -ForegroundColor Yellow
Copy-Item (Join-Path $RepoRoot "README.md") (Join-Path $DocsDir "README.md")
Copy-Item (Join-Path $RepoRoot "LICENSE") (Join-Path $PackageRoot "LICENSE")
Copy-Item (Join-Path $RepoRoot "docs/UnityIntegration.md") (Join-Path $DocsDir "UnityIntegration.md")
Copy-Item (Join-Path $RepoRoot "docs/ThirdPartyLicenses.md") (Join-Path $DocsDir "ThirdPartyLicenses.md")

# Create package.json
Write-Host "📝 Creating package.json..." -ForegroundColor Yellow
$PackageJson = @{
    name = "com.wallstop-studios.novasharp"
    version = $Version
    displayName = "NovaSharp Lua Interpreter"
    description = "Multi-version Lua interpreter (5.1, 5.2, 5.3, 5.4) for Unity. Features comprehensive Lua compatibility, debugging support, bytecode dump/load, and seamless CLR interop. Supports IL2CPP/AOT builds."
    unity = "2021.3"
    unityRelease = "0f1"
    documentationUrl = "https://github.com/wallstop/NovaSharp"
    changelogUrl = "https://github.com/wallstop/NovaSharp/releases"
    licensesUrl = "https://github.com/wallstop/NovaSharp/blob/main/LICENSE"
    license = "MIT"
    keywords = @("lua", "interpreter", "scripting", "modding", "moonsharp")
    author = @{
        name = "Wallstop Studios"
        url = "https://github.com/wallstop"
    }
    repository = @{
        type = "git"
        url = "https://github.com/wallstop/NovaSharp.git"
    }
    samples = @(
        @{
            displayName = "Basic Usage"
            description = "Basic examples of running Lua scripts from C#"
            path = "Samples~/BasicUsage"
        }
    )
}
$PackageJson | ConvertTo-Json -Depth 10 | Set-Content (Join-Path $PackageRoot "package.json") -Encoding UTF8

# Create assembly definitions
$RuntimeAsmdef = @{
    name = "WallstopStudios.NovaSharp.Runtime"
    rootNamespace = "WallstopStudios.NovaSharp"
    references = @()
    includePlatforms = @()
    excludePlatforms = @()
    allowUnsafeCode = $false
    overrideReferences = $true
    precompiledReferences = @(
        "WallstopStudios.NovaSharp.Interpreter.dll",
        "WallstopStudios.NovaSharp.Interpreter.Infrastructure.dll",
        "CommunityToolkit.HighPerformance.dll",
        "Microsoft.Extensions.ObjectPool.dll",
        "System.Text.Json.dll",
        "ZString.dll"
    )
    autoReferenced = $true
    defineConstraints = @()
    versionDefines = @()
    noEngineReferences = $true
}
$RuntimeAsmdef | ConvertTo-Json -Depth 10 | Set-Content (Join-Path $RuntimeDir "WallstopStudios.NovaSharp.Runtime.asmdef") -Encoding UTF8

$DebuggersAsmdef = @{
    name = "WallstopStudios.NovaSharp.Debuggers"
    rootNamespace = "WallstopStudios.NovaSharp"
    references = @("WallstopStudios.NovaSharp.Runtime")
    includePlatforms = @()
    excludePlatforms = @()
    allowUnsafeCode = $false
    overrideReferences = $true
    precompiledReferences = @(
        "WallstopStudios.NovaSharp.RemoteDebugger.dll",
        "WallstopStudios.NovaSharp.VsCodeDebugger.dll"
    )
    autoReferenced = $true
    defineConstraints = @()
    versionDefines = @()
    noEngineReferences = $true
}
$DebuggersAsmdef | ConvertTo-Json -Depth 10 | Set-Content (Join-Path $DebuggersDir "WallstopStudios.NovaSharp.Debuggers.asmdef") -Encoding UTF8

# Create basic sample
$SampleCode = @'
// Basic NovaSharp usage example for Unity
// Copy this file to your Assets folder to use

using UnityEngine;
using WallstopStudios.NovaSharp.Interpreter;

public class NovaSharpBasicUsage : MonoBehaviour
{
    void Start()
    {
        // Create a new Lua script instance
        Script script = new Script();

        // Execute a simple Lua script
        script.DoString(@"
            print('Hello from Lua!')
            
            function greet(name)
                return 'Hello, ' .. name .. '!'
            end
        ");

        // Call a Lua function from C#
        DynValue result = script.Call(script.Globals["greet"], "Unity");
        Debug.Log(result.String); // Outputs: Hello, Unity!

        // Set a global variable accessible from Lua
        script.Globals["unityVersion"] = DynValue.NewString(Application.unityVersion);

        // Execute more Lua code that uses the variable
        script.DoString("print('Running on Unity ' .. unityVersion)");
    }
}
'@
Set-Content (Join-Path $SamplesDir "BasicUsage.cs") $SampleCode -Encoding UTF8

$SampleMeta = @'
fileFormatVersion: 2
guid: a1b2c3d4e5f6789012345678abcdef01
MonoImporter:
  externalObjects: {}
  serializedVersion: 2
  defaultReferences: []
  executionOrder: 0
  icon: {instanceID: 0}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
'@
Set-Content (Join-Path $SamplesDir "BasicUsage.cs.meta") $SampleMeta -Encoding UTF8

# Create CHANGELOG
$Changelog = @"
# Changelog

## [$Version] - $(Get-Date -Format "yyyy-MM-dd")

### Added
- Initial Unity Package Manager release
- NovaSharp Lua interpreter with multi-version support (5.1, 5.2, 5.3, 5.4)
- Remote debugger for browser-based debugging
- VS Code debugger adapter for IDE integration
- Comprehensive CLR interop support
- Sandbox infrastructure with instruction/memory limits

### Notes
- Requires Unity 2021.3 or later
- Supports IL2CPP/AOT builds
- See documentation for detailed usage instructions
"@
Set-Content (Join-Path $PackageRoot "CHANGELOG.md") $Changelog -Encoding UTF8

# Copy LICENSE as LICENSE.md for Unity
Copy-Item (Join-Path $RepoRoot "LICENSE") (Join-Path $PackageRoot "LICENSE.md")

Write-Host ""
Write-Host "✅ Unity package built successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "Package contents:" -ForegroundColor Cyan
Get-ChildItem -Path $PackageRoot -Recurse -File | ForEach-Object {
    Write-Host "  $($_.FullName.Replace($PackageRoot, ''))"
}
Write-Host ""
Write-Host "To use in Unity:" -ForegroundColor Yellow
Write-Host "  1. Open Unity Package Manager (Window > Package Manager)"
Write-Host "  2. Click '+' > Add package from disk..."
Write-Host "  3. Navigate to: $PackageRoot\package.json"
Write-Host ""
Write-Host "Or add to your project's manifest.json:" -ForegroundColor Yellow
Write-Host "  `"com.wallstop-studios.novasharp`": `"file:$PackageRoot`""
