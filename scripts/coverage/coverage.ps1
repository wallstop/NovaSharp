[CmdletBinding()]
param(
    [switch]$SkipBuild,
    [string]$Configuration = "Release",
    [double]$MinimumInterpreterCoverage = 70.0,
    [double]$MinimumInterpreterBranchCoverage = 0.0,
    [double]$MinimumInterpreterMethodCoverage = 0.0,
    [ValidateSet("", "monitor", "enforce")]
    [string]$CoverageGatingMode = $env:COVERAGE_GATING_MODE
)

function Get-CoverageTarget {
    param(
        [string]$Value,
        [double]$Fallback
    )

    if (-not [string]::IsNullOrWhiteSpace($Value)) {
        $parsed = 0.0
        if ([double]::TryParse($Value, [ref]$parsed)) {
            return $parsed
        }
    }

    return $Fallback
}

function Invoke-CoverletRun {
    param(
        [string]$RunnerOutput,
        [string]$TargetArgs,
        [string]$CoverageBase,
        [string]$Label,
        [string[]]$AdditionalArgs = @()
    )

    Write-Host ("Collecting coverage via coverlet ({0})..." -f $Label)

    $arguments = @(
        "tool",
        "run",
        "coverlet",
        $RunnerOutput,
        "--target",
        "dotnet",
        "--targetargs",
        $TargetArgs,
        "--format",
        "json",
        "--format",
        "lcov",
        "--format",
        "cobertura",
        "--format",
        "opencover",
        "--output",
        $CoverageBase,
        "--include",
        "[NovaSharp.*]*",
        "--exclude",
        "[NovaSharp.*Tests*]*"
    )

    if ($AdditionalArgs -and $AdditionalArgs.Count -gt 0) {
        $arguments += $AdditionalArgs
    }

    dotnet @arguments
    if ($LASTEXITCODE -ne 0) {
        throw ("coverlet failed for {0} tests." -f $Label)
    }
}

$ErrorActionPreference = "Stop"
$scriptRoot = $PSScriptRoot
$repoRoot = ""

try {
    $gitRoot = git -C $scriptRoot rev-parse --show-toplevel 2>$null
    if ($LASTEXITCODE -eq 0 -and -not [string]::IsNullOrWhiteSpace($gitRoot)) {
        $repoRoot = $gitRoot.Trim()
    }
}
catch {
    # git might not be available inside some CI containers
}

if ([string]::IsNullOrWhiteSpace($repoRoot)) {
    $current = Get-Item -LiteralPath $scriptRoot
    while ($current -and $current.FullName -ne [System.IO.Path]::GetPathRoot($current.FullName)) {
        if (Test-Path (Join-Path $current.FullName ".git")) {
            $repoRoot = $current.FullName
            break
        }

        if ($current.Parent -eq $null) {
            break
        }

        $current = $current.Parent
    }

    if ([string]::IsNullOrWhiteSpace($repoRoot) -and $current -and $current.Parent) {
        $repoRoot = $current.Parent.FullName
    }
}

if ([string]::IsNullOrWhiteSpace($repoRoot)) {
    $repoRoot = (Resolve-Path ".").Path
}
else {
    $repoRoot = (Resolve-Path -LiteralPath $repoRoot).Path
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
    $tunitRunnerProject = "src/tests/NovaSharp.Interpreter.Tests.TUnit/NovaSharp.Interpreter.Tests.TUnit.csproj"
    $remoteDebuggerRunnerProject = "src/tests/NovaSharp.RemoteDebugger.Tests.TUnit/NovaSharp.RemoteDebugger.Tests.TUnit.csproj"

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

        Write-Host "Building interpreter TUnit project (configuration: $Configuration)..."
        dotnet build $tunitRunnerProject -c $Configuration --no-restore 2>&1 |
            Tee-Object -FilePath $buildLogPath -Append | Out-Null

        if ($LASTEXITCODE -ne 0) {
            Write-Host ""
            Write-Host "dotnet build $tunitRunnerProject failed (exit code $LASTEXITCODE). Showing the last 200 lines:"
            Get-Content $buildLogPath -Tail 200 | ForEach-Object { Write-Host $_ }
            throw (
                "dotnet build {0} -c {1} failed. See {2} for full output." -f
                $tunitRunnerProject,
                $Configuration,
                $buildLogPath
            )
        }

        Write-Host "Building remote debugger test project (configuration: $Configuration)..."
        dotnet build $remoteDebuggerRunnerProject -c $Configuration --no-restore 2>&1 |
            Tee-Object -FilePath $buildLogPath -Append | Out-Null

        if ($LASTEXITCODE -ne 0) {
            Write-Host ""
            Write-Host "dotnet build $remoteDebuggerRunnerProject failed (exit code $LASTEXITCODE). Showing the last 200 lines:"
            Get-Content $buildLogPath -Tail 200 | ForEach-Object { Write-Host $_ }
            throw (
                "dotnet build {0} -c {1} failed. See {2} for full output." -f
                $remoteDebuggerRunnerProject,
                $Configuration,
                $buildLogPath
            )
        }
    }

    $testResultsDir = Join-Path $coverageRoot "test-results"
    New-Item -ItemType Directory -Force -Path $testResultsDir | Out-Null
    $tunitResultsDir = Join-Path $testResultsDir "tunit"
    $remoteDebuggerResultsDir = Join-Path $testResultsDir "remote-debugger"
    New-Item -ItemType Directory -Force -Path $tunitResultsDir | Out-Null
    New-Item -ItemType Directory -Force -Path $remoteDebuggerResultsDir | Out-Null

    $tunitRunnerOutput = Join-Path $repoRoot "src/tests/NovaSharp.Interpreter.Tests.TUnit/bin/$Configuration/net8.0/NovaSharp.Interpreter.Tests.TUnit.dll"
    $remoteDebuggerRunnerOutput = Join-Path $repoRoot "src/tests/NovaSharp.RemoteDebugger.Tests.TUnit/bin/$Configuration/net8.0/NovaSharp.RemoteDebugger.Tests.TUnit.dll"
    if (-not (Test-Path $tunitRunnerOutput)) {
        $message = "TUnit runner output not found at '$tunitRunnerOutput'."
        if ($SkipBuild) {
            $message += " Rerun without -SkipBuild or build the TUnit test project manually."
        }
        else {
            if (Test-Path $buildLogPath) {
                $message += " The preceding build may have failed; inspect $buildLogPath for details."
            }
            else {
                $message += " The preceding build may have failed."
            }
            $message += " You can also run `dotnet build $tunitRunnerProject -c $Configuration` to confirm."
        }

        throw $message
    }

    if (-not (Test-Path $remoteDebuggerRunnerOutput)) {
        $message = "Remote debugger runner output not found at '$remoteDebuggerRunnerOutput'."
        if ($SkipBuild) {
            $message += " Rerun without -SkipBuild or build the remote debugger test project manually."
        }
        else {
            if (Test-Path $buildLogPath) {
                $message += " The preceding build may have failed; inspect $buildLogPath for details."
            }
            else {
                $message += " The preceding build may have failed."
            }
            $message += " You can also run `dotnet build $remoteDebuggerRunnerProject -c $Configuration` to confirm."
        }

        throw $message
    }

    $coverageBase = Join-Path $coverageRoot "coverage"
    $tunitCoverageDir = Join-Path $coverageRoot "tunit"
    New-Item -ItemType Directory -Force -Path $tunitCoverageDir | Out-Null
    $tunitCoverageBase = Join-Path $tunitCoverageDir "coverage"
    $tunitTargetArgs =
        "test --project `"$tunitRunnerProject`" -c $Configuration --no-build --results-directory `"$tunitResultsDir`""
    $remoteDebuggerTargetArgs =
        "test --project `"$remoteDebuggerRunnerProject`" -c $Configuration --no-build --results-directory `"$remoteDebuggerResultsDir`""

    Invoke-CoverletRun -RunnerOutput $tunitRunnerOutput -TargetArgs $tunitTargetArgs -CoverageBase $tunitCoverageBase -Label "TUnit"

    $tunitCoverageJson = "$tunitCoverageBase.json"
    if (-not (Test-Path $tunitCoverageJson)) {
        throw "Coverage report not found at '$tunitCoverageJson' after running TUnit tests."
    }

    $remoteDebuggerMergeArgs = @("--merge-with", $tunitCoverageJson)
    Invoke-CoverletRun `
        -RunnerOutput $remoteDebuggerRunnerOutput `
        -TargetArgs $remoteDebuggerTargetArgs `
        -CoverageBase $coverageBase `
        -Label "RemoteDebugger" `
        -AdditionalArgs $remoteDebuggerMergeArgs

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

        $lineCoverage = [double]$interpreterAssembly.coverage
        $branchCoverage = 0.0
        $methodCoverage = 0.0
        if ($interpreterAssembly.PSObject.Properties.Name -contains "branchcoverage") {
            $branchCoverage = [double]$interpreterAssembly.branchcoverage
        }
        if ($interpreterAssembly.PSObject.Properties.Name -contains "methodcoverage") {
            $methodCoverage = [double]$interpreterAssembly.methodcoverage
        }

        Write-Host ""
        Write-Host ("Interpreter line coverage: {0:N1}%" -f $lineCoverage)
        Write-Host ("Interpreter branch coverage: {0:N1}%" -f $branchCoverage)
        Write-Host ("Interpreter method coverage: {0:N1}%" -f $methodCoverage)

        $gatingMode = ($CoverageGatingMode ?? "").ToLowerInvariant()
        $lineThreshold = $MinimumInterpreterCoverage
        $branchThreshold = $MinimumInterpreterBranchCoverage
        $methodThreshold = $MinimumInterpreterMethodCoverage
        $enforceThresholds = $true

        $lineTarget = Get-CoverageTarget -Value $env:COVERAGE_GATING_TARGET_LINE -Fallback 95.0
        $branchTarget = Get-CoverageTarget -Value $env:COVERAGE_GATING_TARGET_BRANCH -Fallback 95.0
        $methodTarget = Get-CoverageTarget -Value $env:COVERAGE_GATING_TARGET_METHOD -Fallback 95.0

        if ($gatingMode -eq "monitor" -or $gatingMode -eq "enforce") {
            $lineThreshold = [Math]::Max($lineThreshold, $lineTarget)
            $branchThreshold = [Math]::Max($branchThreshold, $branchTarget)
            $methodThreshold = [Math]::Max($methodThreshold, $methodTarget)
            $enforceThresholds = $gatingMode -eq "enforce"
            Write-Host ""
            Write-Host ("Coverage gating mode: {0} (line ≥ {1:N1}%, branch ≥ {2:N1}%, method ≥ {3:N1}%)" -f `
                $gatingMode, $lineThreshold, $branchThreshold, $methodThreshold)
        }

        $violations = @()

        if ($lineThreshold -gt 0 -and $lineCoverage -lt $lineThreshold) {
            $violations += "line coverage {0:N1}% (threshold {1:N1}%)" -f $lineCoverage, $lineThreshold
        }

        if ($branchThreshold -gt 0 -and $branchCoverage -lt $branchThreshold) {
            $violations += "branch coverage {0:N1}% (threshold {1:N1}%)" -f $branchCoverage, $branchThreshold
        }

        if ($methodThreshold -gt 0 -and $methodCoverage -lt $methodThreshold) {
            $violations += "method coverage {0:N1}% (threshold {1:N1}%)" -f $methodCoverage, $methodThreshold
        }

        if ($violations.Count -gt 0) {
            $message =
                "NovaSharp.Interpreter coverage below threshold: {0}" -f `
                ([string]::Join("; ", $violations))

            if ($enforceThresholds) {
                throw $message
            }
            else {
                Write-Warning $message
            }
        }
    }
}
finally {
    Pop-Location
}
