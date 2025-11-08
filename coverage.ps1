[CmdletBinding()]
param(
    [switch]$SkipBuild,
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$scriptPath = $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptPath
if ([string]::IsNullOrWhiteSpace($repoRoot)) {
    $repoRoot = "."
}

Push-Location $repoRoot
try {
    Write-Host "Restoring local tools..."
    dotnet tool restore | Out-Null

    if (-not $SkipBuild) {
        Write-Host "Building solution (configuration: $Configuration)..."
        dotnet build "src/moonsharp.sln" -c $Configuration | Out-Null
    }

    $runnerProject = "src/TestRunners/DotNetCoreTestRunner/DotNetCoreTestRunner.csproj"
    $runnerOutput = Join-Path $repoRoot "src/TestRunners/DotNetCoreTestRunner/bin/$Configuration/net8.0/DotNetCoreTestRunner.dll"
    if (-not (Test-Path $runnerOutput)) {
        throw "Runner output not found at '$runnerOutput'. Build the runner or rerun without -SkipBuild."
    }

    $coverageRoot = Join-Path $repoRoot "artifacts/coverage"
    New-Item -ItemType Directory -Force -Path $coverageRoot | Out-Null

    $coverageBase = Join-Path $coverageRoot "coverage"
    $targetArgs = "run --no-build -c $Configuration --project $runnerProject -- --ci"

    Write-Host "Collecting coverage via coverlet..."
    dotnet tool run coverlet $runnerOutput `
        --target "dotnet" `
        --targetargs $targetArgs `
        --format "lcov" `
        --format "cobertura" `
        --format "opencover" `
        --output $coverageBase `
        --include "[MoonSharp.*]*"

    $reportTarget = Join-Path $repoRoot "docs/coverage/latest"
    if (Test-Path $reportTarget) {
        Remove-Item $reportTarget -Recurse -Force
    }
    New-Item -ItemType Directory -Force -Path $reportTarget | Out-Null

    $coberturaReport = "$coverageBase.cobertura.xml"
    if (-not (Test-Path $coberturaReport)) {
        throw "Coverage report not found at '$coberturaReport'."
    }

    Write-Host "Generating HTML coverage report..."
    dotnet tool run reportgenerator `
        "-reports:$coberturaReport" `
        "-targetdir:$reportTarget" `
        "-reporttypes:Html;TextSummary" `
        "-assemblyfilters:+MoonSharp.*"

    $summaryPath = Join-Path $reportTarget "Summary.txt"
    if (Test-Path $summaryPath) {
        Write-Host ""
        Write-Host (Get-Content $summaryPath)
    }

    Write-Host ""
    Write-Host "Coverage artifacts:"
    Write-Host "  Raw: $coverageRoot"
    Write-Host "  HTML: $reportTarget"
}
finally {
    Pop-Location
}
