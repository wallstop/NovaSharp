[CmdletBinding()]
param(
    [switch]$SkipBuild,
    [string]$Configuration = "Release",
    [double]$MinimumInterpreterCoverage = 70.0
)

$ErrorActionPreference = "Stop"
$scriptPath = $MyInvocation.MyCommand.Path
$scriptDirectory = Split-Path -Parent $scriptPath
$repoRoot = Split-Path -Parent $scriptDirectory
if ([string]::IsNullOrWhiteSpace($repoRoot)) {
    $repoRoot = "."
}

if ([string]::IsNullOrWhiteSpace($env:DOTNET_ROLL_FORWARD)) {
    $env:DOTNET_ROLL_FORWARD = "Major"
    Write-Host "DOTNET_ROLL_FORWARD not set; defaulting to 'Major' so .NET 9 runtimes can host net8 test runners."
}

Push-Location $repoRoot
try {
    Write-Host "Restoring local tools..."
    dotnet tool restore | Out-Null

    $coverageRoot = Join-Path $repoRoot "artifacts/coverage"
    New-Item -ItemType Directory -Force -Path $coverageRoot | Out-Null
    $buildLogPath = Join-Path $coverageRoot "build.log"

    $buildExecuted = $false
    $runnerProject = "src/tests/NovaSharp.Interpreter.Tests/NovaSharp.Interpreter.Tests.csproj"

    if (-not $SkipBuild) {
        Write-Host "Building solution (configuration: $Configuration)..."
        if (Test-Path $buildLogPath) {
            Remove-Item $buildLogPath -Force
        }

        dotnet build "src/NovaSharp.sln" -c $Configuration 2>&1 |
            Tee-Object -FilePath $buildLogPath | Out-Null

        $buildExecuted = $true
        if ($LASTEXITCODE -ne 0) {
            Write-Host ""
            Write-Host "dotnet build failed (exit code $LASTEXITCODE). Showing the last 200 lines:"
            Get-Content $buildLogPath -Tail 200 | ForEach-Object { Write-Host $_ }
            throw (
                "dotnet build src/NovaSharp.sln -c {0} failed. See {1} for full output." -f
                $Configuration,
                $buildLogPath
            )
        }

        Write-Host "Building test project (configuration: $Configuration)..."
        dotnet build $runnerProject -c $Configuration --no-restore 2>&1 |
            Tee-Object -FilePath $buildLogPath -Append | Out-Null

        if ($LASTEXITCODE -ne 0) {
            Write-Host ""
            Write-Host "dotnet build $runnerProject failed (exit code $LASTEXITCODE). Showing the last 200 lines:"
            Get-Content $buildLogPath -Tail 200 | ForEach-Object { Write-Host $_ }
            throw (
                "dotnet build {0} -c {1} failed. See {2} for full output." -f
                $runnerProject,
                $Configuration,
                $buildLogPath
            )
        }
    }

    $testResultsDir = Join-Path $coverageRoot "test-results"
    New-Item -ItemType Directory -Force -Path $testResultsDir | Out-Null

    $runnerOutput = Join-Path $repoRoot "src/tests/NovaSharp.Interpreter.Tests/bin/$Configuration/net8.0/NovaSharp.Interpreter.Tests.dll"
    if (-not (Test-Path $runnerOutput)) {
        $message = "Runner output not found at '$runnerOutput'."
        if ($SkipBuild) {
            $message += " Rerun without -SkipBuild or build the test project manually."
        }
        else {
            if (Test-Path $buildLogPath) {
                $message += " The preceding build may have failed; inspect $buildLogPath for details."
            }
            else {
                $message += " The preceding build may have failed."
            }
            $message += " You can also run `dotnet build src/NovaSharp.sln -c $Configuration` to confirm."
        }

        throw $message
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
    # NOVASHARP_COVERAGE_SUMMARY can be set to 1/true/yes (emit) or 0/false/no (suppress).
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
