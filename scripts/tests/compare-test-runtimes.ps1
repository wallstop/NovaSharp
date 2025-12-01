<#
.SYNOPSIS
    Compares the runtime of two dotnet test invocations (historically NUnit vs. TUnit) and
    emits a JSON artefact with per-suite and per-test timing data.

.DESCRIPTION
    Runs `dotnet test` twice using the provided argument lists, captures the Microsoft.Testing.Platform
    “Detailed” console output for each run, and writes a summary JSON file under
    `artifacts/tunit-migration/<Name>.json`. When the legacy baseline is no longer available, pass
    `-BaselineArtefactPath <existing-json>` to reuse a previously captured NUnit measurement and only run
    the TUnit host. The script automatically enables `--output Detailed`, stores the raw logs under
    `artifacts/tunit-migration/tmp/<label>/<label>.log`, and records the parsed data so migrations can be
    compared without ad-hoc stopwatches.

.EXAMPLE
    # Compare an archived NUnit run with the current TUnit suite.
    pwsh ./scripts/tests/compare-test-runtimes.ps1 `
        -Name remote-debugger-final `
        -BaselineArtefactPath artifacts/tunit-migration/remote-debugger-sample.json `
        -TUnitArguments @(
            "--project", "src/tests/NovaSharp.RemoteDebugger.Tests.TUnit/NovaSharp.RemoteDebugger.Tests.TUnit.csproj",
            "-c", "Release"
        )
#>

[CmdletBinding(DefaultParameterSetName = "RunBaseline")]
param(
    [Parameter(Mandatory = $true)]
    [string]
    $Name,

    [Parameter(Mandatory = $true, ParameterSetName = "RunBaseline")]
    [string[]]
    $NUnitArguments,

    [Parameter(Mandatory = $true, ParameterSetName = "ReuseBaseline")]
    [string]
    $BaselineArtefactPath,

    [Parameter(Mandatory = $true, ParameterSetName = "RunBaseline")]
    [Parameter(Mandatory = $true, ParameterSetName = "ReuseBaseline")]
    [string[]]
    $TUnitArguments,

    [string]
    $DotNetPath = "dotnet",

    [string]
    $OutputDirectory = "artifacts/tunit-migration"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Invoke-TestRun {
    param(
        [string]$Label,
        [string[]]$TestArguments
    )

    $labelSafe = $Label.ToLowerInvariant()
    $resultsDir = Join-Path -Path $TempRoot -ChildPath $labelSafe
    New-Item -ItemType Directory -Force -Path $resultsDir | Out-Null
    $logPath = Join-Path -Path $resultsDir -ChildPath "$labelSafe.log"

    $commandArgs = @("test") + $TestArguments + @("--output", "Detailed", "--results-directory", $resultsDir)
    $joinedArgs = ($commandArgs | ForEach-Object { if ($_ -match '\s') { "`"$_`"" } else { $_ } }) -join " "
    $commandLine = "$DotNetPath $joinedArgs"

    Write-Host ("Running {0}:" -f $Label) -ForegroundColor Cyan
    Write-Host "  $commandLine"

    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    $processOutput = & $DotNetPath $commandArgs 2>&1 | Tee-Object -FilePath $logPath
    $exitCode = $LASTEXITCODE
    $stopwatch.Stop()

    if ($exitCode -ne 0) {
        throw "dotnet test failed for '$Label' with exit code $exitCode."
    }

    $parsed = Parse-TestOutput -Lines $processOutput

    return [pscustomobject]@{
        label = $Label
        command = $commandLine
        totalSeconds = [Math]::Round($stopwatch.Elapsed.TotalSeconds, 4)
        testCount = $parsed.summary.total
        passed = $parsed.summary.succeeded
        failed = $parsed.summary.failed
        skipped = $parsed.summary.skipped
        durationSeconds = $parsed.summary.durationSeconds
        result = $parsed.summary.status
        logPath = (Resolve-Path -LiteralPath $logPath).Path
        tests = $parsed.Tests
    }
}

function ConvertTo-Seconds {
    param(
        [string]$DurationText
    )

    if ([string]::IsNullOrWhiteSpace($DurationText)) {
        return $null
    }

    $totalSeconds = 0.0
    foreach ($part in $DurationText.Trim().Split(" ", [System.StringSplitOptions]::RemoveEmptyEntries)) {
        if ($part -match '^(?<value>\d+(?:\.\d+)?)(?<unit>ms|s|m|h)$') {
            $value = [double]$matches.value
            switch ($matches.unit) {
                "ms" { $totalSeconds += $value / 1000.0 }
                "s" { $totalSeconds += $value }
                "m" { $totalSeconds += $value * 60.0 }
                "h" { $totalSeconds += $value * 3600.0 }
            }
        }
    }

    return [Math]::Round($totalSeconds, 6)
}

function Import-BaselineArtefact {
    param(
        [string]$Path
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        throw "Baseline artefact not found at '$Path'."
    }

    $resolved = (Resolve-Path -LiteralPath $Path).Path
    $json = Get-Content -LiteralPath $resolved -Raw | ConvertFrom-Json
    if ($null -eq $json -or $null -eq $json.nunit) {
        throw "Baseline artefact '$resolved' does not contain an 'nunit' result."
    }

    $nunit = $json.nunit
    return [pscustomobject]@{
        label = if ($nunit.PSObject.Properties.Name -contains "label") { $nunit.label } else { "nunit-archive" }
        command = $nunit.command
        totalSeconds = $nunit.totalSeconds
        testCount = $nunit.testCount
        passed = $nunit.passed
        failed = $nunit.failed
        skipped = $nunit.skipped
        durationSeconds = $nunit.durationSeconds
        result = $nunit.result
        logPath = $nunit.logPath
        tests = $nunit.tests
        sourceArtefact = $resolved
    }
}

function Parse-TestOutput {
    param(
        [string[]]$Lines
    )

    if ($Lines -eq $null) {
        $Lines = @()
    }

    $tests = @()
    $testPattern = '^(?<status>[A-Za-z]+)\s+(?<name>.+?)\s+\((?<duration>[^\)]+)\)$'

    foreach ($line in $Lines) {
        $trimmed = $line.Trim()
        if ($trimmed -match $testPattern) {
            $status = $matches.status.ToLowerInvariant()
            if ($status -notin @("passed", "failed", "skipped")) {
                continue
            }

            $name = $matches.name.Trim()
            $durationText = $matches.duration.Trim()
            $durationSeconds = ConvertTo-Seconds -DurationText $durationText

            $tests += [pscustomobject]@{
                name = $name
                outcome = $status
                durationSeconds = $durationSeconds
                rawDuration = $durationText
            }
        }
    }

    $summaryIndex = -1
    for ($i = 0; $i -lt $Lines.Count; $i++) {
        if ($Lines[$i].StartsWith("Test run summary:", [System.StringComparison]::OrdinalIgnoreCase)) {
            $summaryIndex = $i
            break
        }
    }

    $statusText = $null
    $total = 0
    $failed = 0
    $succeeded = 0
    $skipped = 0
    $durationSeconds = $null

    if ($summaryIndex -ge 0) {
        $statusLine = $Lines[$summaryIndex].Trim()
        if ($statusLine -match '^Test run summary:\s+(?<result>.+)$') {
            $statusText = $matches.result.Trim()
        }

        for ($j = $summaryIndex + 1; $j -lt $Lines.Count; $j++) {
            $line = $Lines[$j].Trim()
            if ($line.Length -eq 0) {
                continue
            }

            if ($line -match '^total:\s+(?<value>\d+)') {
                $total = [int]$matches.value
            }
            elseif ($line -match '^failed:\s+(?<value>\d+)') {
                $failed = [int]$matches.value
            }
            elseif ($line -match '^succeeded:\s+(?<value>\d+)') {
                $succeeded = [int]$matches.value
            }
            elseif ($line -match '^skipped:\s+(?<value>\d+)') {
                $skipped = [int]$matches.value
            }
            elseif ($line -match '^duration:\s+(?<value>.+)$') {
                $durationSeconds = ConvertTo-Seconds -DurationText $matches.value.Trim()
            }
            else {
                # Once we encounter a line that does not belong to the summary block, stop parsing.
                if (-not $line.StartsWith("  ")) {
                    break
                }
            }
        }
    }

    return @{
        Tests = $tests
        summary = @{
            status = $statusText
            total = $total
            failed = $failed
            succeeded = $succeeded
            skipped = $skipped
            durationSeconds = $durationSeconds
        }
    }
}

$outputRoot = Join-Path -Path (Get-Location) -ChildPath $OutputDirectory
New-Item -ItemType Directory -Force -Path $outputRoot | Out-Null

$TempRoot = Join-Path -Path $outputRoot -ChildPath "tmp"
New-Item -ItemType Directory -Force -Path $TempRoot | Out-Null

$nunitResult = $null
if ($PSCmdlet.ParameterSetName -eq "RunBaseline") {
    $nunitResult = Invoke-TestRun -Label "nunit" -TestArguments $NUnitArguments
}
else {
    $nunitResult = Import-BaselineArtefact -Path $BaselineArtefactPath
}
$tunitResult = Invoke-TestRun -Label "tunit" -TestArguments $TUnitArguments

$sanitizedName = ($Name -replace '[^A-Za-z0-9_.-]', "-").ToLowerInvariant()
$artefactPath = Join-Path -Path $outputRoot -ChildPath ("{0}.json" -f $sanitizedName)

$summary = [pscustomobject]@{
    name = $Name
    generatedOn = (Get-Date).ToString("o")
    outputDirectory = (Resolve-Path -LiteralPath $outputRoot).Path
    artefact = $artefactPath
    nunit = $nunitResult
    tunit = $tunitResult
    deltaSeconds = [Math]::Round($tunitResult.totalSeconds - $nunitResult.totalSeconds, 4)
}

if ($PSCmdlet.ParameterSetName -eq "ReuseBaseline" -and $nunitResult.PSObject.Properties.Name -contains "sourceArtefact") {
    $summary | Add-Member -NotePropertyName baselineArtefact -NotePropertyValue $nunitResult.sourceArtefact
}

$json = $summary | ConvertTo-Json -Depth 6
Set-Content -LiteralPath $artefactPath -Value $json -Encoding UTF8

Write-Host ""
Write-Host "Comparison summary written to $($summary.artefact)" -ForegroundColor Green
Write-Host ("NUnit: {0} s | TUnit: {1} s | Delta: {2} s" -f $nunitResult.totalSeconds, $tunitResult.totalSeconds, $summary.deltaSeconds)
