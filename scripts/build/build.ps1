[CmdletBinding()]
param(
    [string]$Configuration = "Release",
    [string]$Solution = "src/NovaSharp.sln",
    [string]$TestProject = "src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.csproj",
    [switch]$SkipTests,
    [switch]$SkipToolRestore,
    [switch]$SkipRestore
)

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
    # git may be unavailable in some minimal environments
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
    Write-Host "DOTNET_ROLL_FORWARD not set; defaulting to 'Major' so .NET 9 hosts can run the net8 test runner."
}

# Suppress TUnit ASCII banner and telemetry messages
$env:TESTINGPLATFORM_NOBANNER = "1"
$env:DOTNET_CLI_TELEMETRY_OPTOUT = "1"

Push-Location $repoRoot
try {
    if (-not $SkipToolRestore) {
        Write-Host "Restoring local dotnet tools..."
        dotnet tool restore
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet tool restore failed with exit code $LASTEXITCODE."
        }
    }

    Write-Host "Building $Solution (configuration: $Configuration)..."
    $restoreArgs = @()
    if ($SkipRestore) {
        $restoreArgs += "--no-restore"
    }

    dotnet build $Solution -c $Configuration /m @restoreArgs
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet build $Solution -c $Configuration failed with exit code $LASTEXITCODE."
    }

    if (-not $SkipTests) {
        $resultsDir = Join-Path $repoRoot "artifacts/test-results"
        if (-not (Test-Path $resultsDir)) {
            New-Item -ItemType Directory -Force -Path $resultsDir | Out-Null
        }

        Write-Host "Running tests for $TestProject..."
        dotnet test $TestProject -c $Configuration --no-build `
            --logger "trx;LogFileName=NovaSharpInterpreterTUnit.trx" `
            --results-directory $resultsDir
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet test $TestProject failed with exit code $LASTEXITCODE."
        }
    }

    Write-Host ""
    Write-Host "Build helper completed successfully."
}
finally {
    Pop-Location
}
