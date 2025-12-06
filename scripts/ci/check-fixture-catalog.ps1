<#
.SYNOPSIS
    Ensures FixtureCatalogGenerated.cs matches the output of update-fixture-catalog.ps1.

.DESCRIPTION
    Runs ./scripts/tests/update-fixture-catalog.ps1 from the repo root and verifies that
    src/tests/WallstopStudios.NovaSharp.Interpreter.Tests/FixtureCatalogGenerated.cs is unchanged. CI should
    invoke this script to guarantee contributors regenerate the catalog when fixtures move.

.USAGE
    pwsh ./scripts/ci/check-fixture-catalog.ps1
#>

param(
    [string]$RepoRoot = (Resolve-Path "$PSScriptRoot/../..").Path,
    [string]$FixtureCatalog = "src/tests/WallstopStudios.NovaSharp.Interpreter.Tests/FixtureCatalogGenerated.cs",
    [string]$GeneratorScript = "scripts/tests/update-fixture-catalog.ps1"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

Push-Location $RepoRoot
try {
    $generatorPath = Join-Path $RepoRoot $GeneratorScript
    if (-not (Test-Path $generatorPath)) {
        throw "Fixture catalog generator script not found at $generatorPath"
    }

    Write-Host "Regenerating fixture catalog via $GeneratorScript..."
    & pwsh -NoLogo -NoProfile -File $generatorPath | Write-Host

    $status = git status --short -- $FixtureCatalog
    if ($LASTEXITCODE -ne 0) {
        throw "git status failed when inspecting $FixtureCatalog"
    }

    if ($status) {
        git diff -- $FixtureCatalog | Write-Host
        throw "FixtureCatalogGenerated.cs is out of date. Run pwsh ./scripts/tests/update-fixture-catalog.ps1 and commit the result."
    }

    Write-Host "Fixture catalog is up to date."
}
finally {
    Pop-Location
}


