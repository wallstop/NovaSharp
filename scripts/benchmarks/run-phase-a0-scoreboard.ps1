[CmdletBinding()]
param(
    [string]$Configuration = "Release",
    [string]$Output = "artifacts/phase-a0-scoreboard.md",
    [string]$PhaseBaseline = "progress/benchmarks/phase-a0-scoreboard-baseline.json",
    [string]$WritePhaseBaseline = "",
    [switch]$EnforcePhaseGates,
    [switch]$SkipLuaCli
)

$ErrorActionPreference = "Stop"

function Resolve-RepoRoot {
    param([string]$StartPath)

    $current = Get-Item -LiteralPath $StartPath
    while ($null -ne $current) {
        if (Test-Path (Join-Path $current.FullName ".git")) {
            return $current.FullName
        }
        if ($null -eq $current.Parent) {
            break
        }
        $current = $current.Parent
    }

    return (Resolve-Path ".").Path
}

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

    throw "Python 3 is required to render the Phase A0 scoreboard. Set PYTHON to override."
}

$scriptRoot = $PSScriptRoot
$repoRoot = Resolve-RepoRoot -StartPath $scriptRoot
Set-Location $repoRoot

if ([string]::IsNullOrWhiteSpace($env:DOTNET_ROLL_FORWARD)) {
    $env:DOTNET_ROLL_FORWARD = "Major"
    Write-Host "DOTNET_ROLL_FORWARD not set; defaulting to 'Major' so .NET 9 hosts can execute the .NET 8 benchmarks."
}

$comparisonProject = "src/tooling/WallstopStudios.NovaSharp.Comparison/WallstopStudios.NovaSharp.Comparison.csproj"
$comparisonArtifacts = "artifacts/benchmarkdotnet/phase-a0-comparison"
$luaCliScenarios = "artifacts/benchmarkdotnet/phase-a0-lua-cli-scenarios"

if (Test-Path $comparisonArtifacts) {
    Remove-Item -LiteralPath $comparisonArtifacts -Recurse -Force
}
if (Test-Path $luaCliScenarios) {
    Remove-Item -LiteralPath $luaCliScenarios -Recurse -Force
}
New-Item -ItemType Directory -Path $comparisonArtifacts -Force | Out-Null

Write-Host "Restoring local tools..."
dotnet tool restore
if ($LASTEXITCODE -ne 0) {
    throw "dotnet tool restore failed."
}

Write-Host "Building comparison benchmarks (configuration: $Configuration)..."
dotnet build $comparisonProject -c $Configuration
if ($LASTEXITCODE -ne 0) {
    throw "dotnet build $comparisonProject -c $Configuration failed."
}

Write-Host ""
Write-Host "Running Phase A0 comparison benchmarks..."
dotnet run `
    --project $comparisonProject `
    -c $Configuration `
    --no-build `
    -- `
    --filter "*" `
    --artifacts $comparisonArtifacts
if ($LASTEXITCODE -ne 0) {
    throw "dotnet run --project $comparisonProject -c $Configuration failed. See output above."
}

if (-not $SkipLuaCli) {
    Write-Host ""
    Write-Host "Exporting Phase A0 scenarios for reference lua CLI context..."
    dotnet run `
        --project $comparisonProject `
        -c $Configuration `
        --no-build `
        -- `
        --export-scenarios $luaCliScenarios
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet run --project $comparisonProject -c $Configuration -- --export-scenarios failed. See output above."
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

$rendererArgs = @(
    "scripts/benchmarks/render-benchmark-deltas.py",
    "--current-root", "BenchmarkDotNet.Artifacts",
    "--comparison-root", $comparisonArtifacts,
    "--phase-baseline", $PhaseBaseline,
    "--output", $Output
)
if (-not $SkipLuaCli) {
    $rendererArgs += "--expect-lua-cli"
}
if (-not [string]::IsNullOrWhiteSpace($WritePhaseBaseline)) {
    $rendererArgs += @("--write-phase-baseline", $WritePhaseBaseline)
}
if ($EnforcePhaseGates) {
    $rendererArgs += "--enforce-phase-gates"
}

Write-Host ""
Write-Host "Rendering Phase A0 scoreboard..."
$pythonCommand = Resolve-PythonCommand
& $pythonCommand @rendererArgs
if ($LASTEXITCODE -ne 0) {
    throw "scripts/benchmarks/render-benchmark-deltas.py failed. See output above."
}

Write-Host ""
Write-Host "Phase A0 comparison artifacts:"
Write-Host "  $comparisonArtifacts"
Write-Host "Phase A0 scoreboard:"
Write-Host "  $Output"
