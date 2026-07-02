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

$comparisonArtifacts = "artifacts/benchmarkdotnet/comparison"
$luaCliScenarios = "artifacts/benchmarkdotnet/lua-cli-scenarios"
if (Test-Path $comparisonArtifacts) {
    Remove-Item -LiteralPath $comparisonArtifacts -Recurse -Force
}
if (Test-Path $luaCliScenarios) {
    Remove-Item -LiteralPath $luaCliScenarios -Recurse -Force
}

if (-not $SkipComparison) {
    New-Item -ItemType Directory -Path $comparisonArtifacts -Force | Out-Null

    Write-Host ""
    Write-Host "Running comparison benchmarks..."
    dotnet run `
        --project $ComparisonBenchmarkProject `
        -c $Configuration `
        --no-build `
        -- `
        --filter "*" `
        --exporters json `
        --artifacts $comparisonArtifacts
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet run --project $ComparisonBenchmarkProject -c $Configuration failed. See output above."
    }
    Write-Host "comparison benchmarks complete."

    Write-Host ""
    Write-Host "Exporting comparison scenarios for reference lua CLI context..."
    dotnet run `
        --project $ComparisonBenchmarkProject `
        -c $Configuration `
        --no-build `
        -- `
        --export-scenarios $luaCliScenarios
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet run --project $ComparisonBenchmarkProject -c $Configuration -- --export-scenarios failed. See output above."
    }

    Write-Host ""
    Write-Host "Measuring reference lua CLI wall-time context..."
    $pythonCommand = Resolve-PythonCommand
    & $pythonCommand "scripts/benchmarks/run-lua-cli-context.py" `
        "--scenario-dir" $luaCliScenarios `
        "--output-root" $comparisonArtifacts
    if ($LASTEXITCODE -ne 0) {
        throw "scripts/benchmarks/run-lua-cli-context.py failed. See output above."
    }
}
else {
    Write-Host ""
    Write-Host "Skipping comparison benchmarks (per -SkipComparison)."
}

Write-Host ""
Write-Host "Rendering benchmark comparison delta report..."
$pythonCommand = Resolve-PythonCommand
& $pythonCommand "scripts/benchmarks/render-benchmark-deltas.py" `
    "--current-root" "BenchmarkDotNet.Artifacts" `
    "--comparison-root" $comparisonArtifacts `
    "--self-baseline-root" "docs/performance-history/current-baseline" `
    "--output" "artifacts/benchmark-deltas.md"
if ($LASTEXITCODE -ne 0) {
    throw "scripts/benchmarks/render-benchmark-deltas.py failed. See output above."
}

$artifactsPath = Join-Path $repoRoot "BenchmarkDotNet.Artifacts"
$comparisonArtifactsPath = Join-Path $repoRoot $comparisonArtifacts
Write-Host ""
Write-Host "BenchmarkDotNet artifacts are available under:"
Write-Host "  $artifactsPath"
Write-Host "Comparison BenchmarkDotNet artifacts are available under:"
Write-Host "  $comparisonArtifactsPath"
Write-Host "Benchmark comparison delta report:"
Write-Host "  artifacts/benchmark-deltas.md"
Write-Host ""
Write-Host "Review the updated sections in docs/Performance.md and attach relevant artifacts when opening a PR."
