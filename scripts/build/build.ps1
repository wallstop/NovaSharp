[CmdletBinding()]
param(
    [string]$Configuration = "Release",
    [string]$Solution = "src/NovaSharp.sln",
    [string]$TestProject = "src/tests/NovaSharp.Interpreter.Tests/NovaSharp.Interpreter.Tests.csproj",
    [switch]$SkipTests,
    [switch]$SkipToolRestore
)

$ErrorActionPreference = "Stop"
$scriptPath = $MyInvocation.MyCommand.Path
$scriptDirectory = Split-Path -Parent $scriptPath
$repoRoot = Split-Path -Parent $scriptDirectory
if (-not [string]::IsNullOrWhiteSpace($repoRoot)) {
    $repoRoot = Split-Path -Parent $repoRoot
}
if ([string]::IsNullOrWhiteSpace($repoRoot)) {
    $repoRoot = "."
}

if ([string]::IsNullOrWhiteSpace($env:DOTNET_ROLL_FORWARD)) {
    $env:DOTNET_ROLL_FORWARD = "Major"
    Write-Host "DOTNET_ROLL_FORWARD not set; defaulting to 'Major' so .NET 9 hosts can run the net8 test runner."
}

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
    dotnet build $Solution -c $Configuration
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet build $Solution -c $Configuration failed with exit code $LASTEXITCODE."
    }

    if (-not $SkipTests) {
        $resultsDir = Join-Path $repoRoot "artifacts/test-results"
        if (-not (Test-Path $resultsDir)) {
            New-Item -ItemType Directory -Force -Path $resultsDir | Out-Null
        }

        Write-Host "Running tests for $TestProject..."
        dotnet test $TestProject -c $Configuration --no-build --logger "trx;LogFileName=NovaSharpTests.trx" --results-directory $resultsDir
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
