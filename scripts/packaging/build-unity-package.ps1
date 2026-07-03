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

$PackageTemplateRoot = Join-Path $RepoRoot "src/unity/com.wallstop-studios.novasharp"
$SamplesSource = Join-Path $PackageTemplateRoot "Samples~"
$PackageRoot = Join-Path $OutputPath "com.wallstop-studios.novasharp"
$RuntimeDir = Join-Path $PackageRoot "Runtime"
$DebuggersDir = Join-Path $RuntimeDir "Debuggers"
$EditorDir = Join-Path $PackageRoot "Editor"
$DocsDir = Join-Path $PackageRoot "Documentation~"
$SamplesRoot = Join-Path $PackageRoot "Samples~"
$PathTrimChars = [char[]]@([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)

function Test-PathWithinOrEqual {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Candidate,

        [Parameter(Mandatory = $true)]
        [string]$Root
    )

    $candidateFullPath = [System.IO.Path]::GetFullPath($Candidate).TrimEnd($PathTrimChars)
    $rootFullPath = [System.IO.Path]::GetFullPath($Root).TrimEnd($PathTrimChars)

    if ([string]::Equals($candidateFullPath, $rootFullPath, [System.StringComparison]::OrdinalIgnoreCase)) {
        return $true
    }

    return $candidateFullPath.StartsWith(
        $rootFullPath + [System.IO.Path]::DirectorySeparatorChar,
        [System.StringComparison]::OrdinalIgnoreCase)
}

function Test-PathOverlap {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Left,

        [Parameter(Mandatory = $true)]
        [string]$Right
    )

    return (Test-PathWithinOrEqual -Candidate $Left -Root $Right) -or (Test-PathWithinOrEqual -Candidate $Right -Root $Left)
}

if (-not (Test-Path $SamplesSource)) {
    throw "Missing Unity package sample templates: $SamplesSource"
}

if (Test-PathOverlap -Left $PackageRoot -Right $PackageTemplateRoot) {
    throw "Unity package output must not overlap tracked package templates: $PackageRoot"
}

if (Test-PathOverlap -Left $SamplesRoot -Right $SamplesSource) {
    throw "Unity package sample output must not overlap tracked sample templates: $SamplesRoot"
}

# Create output directories
New-Item -ItemType Directory -Path $RuntimeDir -Force | Out-Null
New-Item -ItemType Directory -Path $DebuggersDir -Force | Out-Null
New-Item -ItemType Directory -Path $EditorDir -Force | Out-Null
New-Item -ItemType Directory -Path $DocsDir -Force | Out-Null

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
        },
        @{
            displayName = "IL2CPP Spot Check"
            description = "Minimal stopwatch scene for smoke-testing NovaSharp in IL2CPP player builds"
            path = "Samples~/IL2CPPSpotCheck"
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

# Copy package samples from tracked templates.
if (Test-Path $SamplesRoot) {
    Remove-Item -LiteralPath $SamplesRoot -Recurse -Force
}
New-Item -ItemType Directory -Path $SamplesRoot -Force | Out-Null
Copy-Item -Path (Join-Path $SamplesSource "*") -Destination $SamplesRoot -Recurse -Force

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
