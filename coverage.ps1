[CmdletBinding()]
param(
    [switch]$SkipBuild,
    [string]$Configuration = "Release",
    [double]$MinimumInterpreterCoverage = 70.0
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
        dotnet build "src/NovaSharp.sln" -c $Configuration | Out-Null
    }

    $runnerProject = "src/tests/NovaSharp.Interpreter.Tests/NovaSharp.Interpreter.Tests.csproj"
    $coverageRoot = Join-Path $repoRoot "artifacts/coverage"
    New-Item -ItemType Directory -Force -Path $coverageRoot | Out-Null

    $testResultsDir = Join-Path $coverageRoot "test-results"
    New-Item -ItemType Directory -Force -Path $testResultsDir | Out-Null

    $runnerOutput = Join-Path $repoRoot "src/tests/NovaSharp.Interpreter.Tests/bin/$Configuration/net8.0/NovaSharp.Interpreter.Tests.dll"
    if (-not (Test-Path $runnerOutput)) {
        throw "Runner output not found at '$runnerOutput'. Build the runner or rerun without -SkipBuild."
    }

    $coverageBase = Join-Path $coverageRoot "coverage"
    $targetArgs =
        "test `"$runnerProject`" -c $Configuration --no-build --logger `"trx;LogFileName=NovaSharpTests.trx`" --results-directory `"$testResultsDir`""

    Write-Host "Collecting coverage via coverlet..."
    dotnet tool run coverlet $runnerOutput `
        --target "dotnet" `
        --targetargs $targetArgs `
        --format "lcov" `
        --format "cobertura" `
        --format "opencover" `
        --output $coverageBase `
        --include "[NovaSharp.*]*" `
        --exclude "[NovaSharp.*Tests*]*"

    $reportTarget = Join-Path $repoRoot "docs/coverage/latest"
    if (Test-Path $reportTarget) {
        Remove-Item $reportTarget -Recurse -Force
    }
    New-Item -ItemType Directory -Force -Path $reportTarget | Out-Null

    $coberturaReport = "$coverageBase.cobertura.xml"
    if (-not (Test-Path $coberturaReport)) {
        throw "Coverage report not found at '$coberturaReport'."
    }

    Write-Host "Generating coverage report set (HTML, text, markdown, JSON)..."
    $reportTypes = "Html;TextSummary;MarkdownSummary;MarkdownSummaryGithub;JsonSummary"
    dotnet tool run reportgenerator `
        "-reports:$coberturaReport" `
        "-targetdir:$reportTarget" `
        "-reporttypes:$reportTypes" `
        "-assemblyfilters:+NovaSharp.*"

function ShouldEmitFullCoverageSummary {
    $override = [System.Environment]::GetEnvironmentVariable("NOVASHARP_COVERAGE_SUMMARY")
    if (-not [string]::IsNullOrWhiteSpace($override)) {
        switch ($override.ToLowerInvariant()) {
            "1" { return $true }
            "true" { return $true }
            "yes" { return $true }
            "0" { return $false }
            "false" { return $false }
            "no" { return $false }
        }
    }

    $ciMarkers = @(
        "CI",
        "GITHUB_ACTIONS",
        "TF_BUILD",
        "TEAMCITY_VERSION",
        "BUILD_BUILDID",
        "APPVEYOR"
    )

    foreach ($marker in $ciMarkers) {
        if (-not [string]::IsNullOrWhiteSpace([System.Environment]::GetEnvironmentVariable($marker))) {
            return $true
        }
    }

    return $false
}

$summaryPath = Join-Path $reportTarget "Summary.txt"
if (Test-Path $summaryPath) {
    Write-Host ""

    if (ShouldEmitFullCoverageSummary) {
        Write-Host (Get-Content $summaryPath)
    }
    else {
        $content = Get-Content $summaryPath
        $header = New-Object System.Collections.Generic.List[string]
        foreach ($line in $content) {
            if ([string]::IsNullOrWhiteSpace($line) -and $header.Count -gt 0) {
                break
            }

            $header.Add($line)
        }

        if ($header.Count -gt 0) {
            Write-Host ($header -join [Environment]::NewLine)
            Write-Host ""
        }

        Write-Host "Detailed coverage summary (assemblies/methods) saved to: $summaryPath"
    }
}

    Write-Host ""
    Write-Host "Coverage artifacts:"
    Write-Host "  Raw: $coverageRoot"
    Write-Host "  HTML: $reportTarget"

    $exportTargets = @(
        "Summary.txt",
        "Summary.md",
        "SummaryGithub.md",
        "Summary.json"
    )

    foreach ($fileName in $exportTargets) {
        $sourcePath = Join-Path $reportTarget $fileName
        if (Test-Path $sourcePath) {
            Copy-Item -Path $sourcePath -Destination (Join-Path $coverageRoot $fileName) -Force
        }
    }

    $summaryGithubPath = Join-Path $reportTarget "SummaryGithub.md"
    if (Test-Path $summaryGithubPath) {
        Write-Host "  Summary (GitHub): $summaryGithubPath"
    }

    $summaryJsonPath = Join-Path $reportTarget "Summary.json"
    if (Test-Path $summaryJsonPath) {
        $summaryData = Get-Content $summaryJsonPath -Raw | ConvertFrom-Json
        $interpreterAssembly = $summaryData.coverage.assemblies |
            Where-Object { $_.name -eq "NovaSharp.Interpreter" }

        if ($null -eq $interpreterAssembly) {
            throw "Interpreter assembly not found in coverage summary. Verify reportgenerator configuration."
        }

        $interpreterCoverage = [double]$interpreterAssembly.coverage
        Write-Host ""
        Write-Host ("Interpreter line coverage: {0:N1}%" -f $interpreterCoverage)
        if ($interpreterCoverage -lt $MinimumInterpreterCoverage) {
            throw (
                "NovaSharp.Interpreter line coverage {0:N1}% is below the required {1:N1}% threshold." -f
                $interpreterCoverage,
                $MinimumInterpreterCoverage
            )
        }
    }
}
finally {
    Pop-Location
}
