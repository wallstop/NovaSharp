[CmdletBinding()]
param(
    [string]$Configuration = "Release",
    [string]$RuntimeBenchmarkProject = "src/tooling/WallstopStudios.NovaSharp.Benchmarks/WallstopStudios.NovaSharp.Benchmarks.csproj",
    [string]$ComparisonBenchmarkProject = "src/tooling/WallstopStudios.NovaSharp.Comparison/WallstopStudios.NovaSharp.Comparison.csproj",
    [switch]$SkipComparison
)

$ErrorActionPreference = "Stop"

function Resolve-RepoRoot {
    param([string]$StartPath)

    $current = Get-Item -LiteralPath $StartPath
    while ($null -ne $current) {
        if (Test-Path (Join-Path $current.FullName ".git")) {
            return $current.FullName
        }
        if ($current.Parent -eq $null) {
            break
        }
        $current = $current.Parent
    }

    return (Resolve-Path ".").Path
}

$scriptRoot = $PSScriptRoot
$repoRoot = Resolve-RepoRoot -StartPath $scriptRoot
Set-Location $repoRoot

function Resolve-PythonCommand {
    if (-not [string]::IsNullOrWhiteSpace($env:PYTHON)) {
        return $env:PYTHON
    }

    $python3 = Get-Command python3 -ErrorAction SilentlyContinue
    if ($null -ne $python3) {
        return $python3.Source
    }

    $python = Get-Command python -ErrorAction SilentlyContinue
    if ($null -ne $python) {
        return $python.Source
    }

    throw "Python 3 is required to render the benchmark delta report. Set PYTHON to override."
}

if ([string]::IsNullOrWhiteSpace($env:DOTNET_ROLL_FORWARD)) {
    $env:DOTNET_ROLL_FORWARD = "Major"
    Write-Host "DOTNET_ROLL_FORWARD not set; defaulting to 'Major' so .NET 9 hosts can execute the .NET 8 benchmarks."
}

Write-Host "Restoring local tools..."
dotnet tool restore
if ($LASTEXITCODE -ne 0) {
    throw "dotnet tool restore failed."
}

Write-Host "Building solution (configuration: $Configuration)..."
dotnet build "src/NovaSharp.sln" -c $Configuration
if ($LASTEXITCODE -ne 0) {
    throw "dotnet build src/NovaSharp.sln -c $Configuration failed."
}

function Invoke-Benchmark {
    param(
        [string]$Project,
        [string]$Configuration,
        [string]$Description
    )

    Write-Host ""
    Write-Host "Running $Description benchmarks..."
    dotnet run --project $Project -c $Configuration --no-build
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet run --project $Project -c $Configuration failed. See output above."
    }
    Write-Host "$Description benchmarks complete."
}

Invoke-Benchmark -Project $RuntimeBenchmarkProject -Configuration $Configuration -Description "NovaSharp runtime"

if (-not $SkipComparison) {
    Invoke-Benchmark -Project $ComparisonBenchmarkProject -Configuration $Configuration -Description "comparison"
}
else {
    Write-Host ""
    Write-Host "Skipping comparison benchmarks (per -SkipComparison)."
}

Write-Host ""
Write-Host "Rendering MoonSharp benchmark delta report..."
$pythonCommand = Resolve-PythonCommand
& $pythonCommand "scripts/benchmarks/render-benchmark-deltas.py" `
    "--current-root" "BenchmarkDotNet.Artifacts" `
    "--baseline-doc" "docs/Performance.md" `
    "--output" "artifacts/benchmark-deltas.md"
if ($LASTEXITCODE -ne 0) {
    throw "scripts/benchmarks/render-benchmark-deltas.py failed. See output above."
}

$artifactsPath = Join-Path $repoRoot "BenchmarkDotNet.Artifacts"
Write-Host ""
Write-Host "BenchmarkDotNet artifacts are available under:"
Write-Host "  $artifactsPath"
Write-Host "MoonSharp benchmark delta report:"
Write-Host "  artifacts/benchmark-deltas.md"
Write-Host ""
Write-Host "Review the updated sections in docs/Performance.md and attach relevant artifacts when opening a PR."
