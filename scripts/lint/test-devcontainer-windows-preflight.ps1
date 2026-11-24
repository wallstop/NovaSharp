# Exercise the host command with the Windows path expansion performed by the CLI.
$ErrorActionPreference = 'Stop'
$repo = Get-Item (Join-Path $PSScriptRoot '../..')
$config = Get-Content -LiteralPath (Join-Path $repo '.devcontainer/devcontainer.json') -Raw | ConvertFrom-Json
$fixture = [IO.Directory]::CreateTempSubdirectory('NovaSharp preflight ')
$workspace = $fixture.CreateSubdirectory('checkout with spaces')
$helpers = $workspace.CreateSubdirectory('.devcontainer')
$sourceFile = Join-Path $workspace '.env.local'
$envFile = Join-Path $helpers '.mcp.env'
$currentSid = [Security.Principal.WindowsIdentity]::GetCurrent().User.Value

function Assert-PrivateAcl($path) {
    $acl = Get-Acl -LiteralPath $path
    if (!$acl.AreAccessRulesProtected) { throw "Inherited permissions on $path" }
    if ($acl.GetOwner([Security.Principal.SecurityIdentifier]).Value -ne $currentSid) {
        throw "Wrong owner on $path"
    }
    $rules = $acl.GetAccessRules($true, $true, [Security.Principal.SecurityIdentifier])
    if ($rules.Count -ne 1 -or $rules[0].IdentityReference.Value -ne $currentSid -or
        $rules[0].AccessControlType -ne 'Allow' -or $rules[0].FileSystemRights -ne 'FullControl') {
        throw "Expected a single access rule granting the current user full control of $path"
    }
}

function Invoke-Preflight([switch]$ShouldFail) {
    $command, [string[]] $arguments = $config.initializeCommand | ForEach-Object {
        $_.Replace('${localWorkspaceFolder}', $workspace.FullName)
    }
    $output = & $command @arguments 2>&1
    $result = $LASTEXITCODE
    if (($output -join "`n") -match 'synthetic-(zai|github|unrelated)') {
        throw 'Host preflight logged a credential'
    }
    if (($result -ne 0) -ne $ShouldFail.IsPresent) {
        throw "Unexpected preflight exit $result. $output"
    }
}

try {
    Copy-Item (Join-Path $repo '.devcontainer/check-mcp-env.sh') $helpers.FullName
    Copy-Item (Join-Path $repo '.devcontainer/mcp-env-windows.ps1') $helpers.FullName
    Push-Location -LiteralPath $workspace.FullName
    try {
        Invoke-Preflight
        if ((Get-Item -LiteralPath $envFile).Length -ne 0) { throw 'Expected an empty Docker env file' }

        $expected = @('GITHUB_PERSONAL_ACCESS_TOKEN=synthetic-github', 'ZAI_API_KEY=synthetic-zai')
        [IO.File]::WriteAllLines($sourceFile, $expected + 'OTHER=synthetic-unrelated')
        $acl = Get-Acl -LiteralPath $sourceFile
        $acl.AddAccessRule([Security.AccessControl.FileSystemAccessRule]::new(
            [Security.Principal.SecurityIdentifier]::new('S-1-5-32-545'), 'Read', 'Allow'))
        Set-Acl -LiteralPath $sourceFile -AclObject $acl
        Invoke-Preflight
        Assert-PrivateAcl $sourceFile
        Assert-PrivateAcl $envFile
        if ((Get-Content -LiteralPath $envFile) -join "`n" -cne ($expected -join "`n")) {
            throw 'Docker env file contains the wrong assignments'
        }

        [IO.File]::AppendAllText($sourceFile, "ZAI_API_KEY=duplicate`n")
        Invoke-Preflight -ShouldFail
        if ((Get-Content -LiteralPath $envFile) -join "`n" -cne ($expected -join "`n")) {
            throw 'Invalid input changed the last published file'
        }
        Remove-Item -LiteralPath $sourceFile
        Invoke-Preflight
        if ((Get-Item -LiteralPath $envFile).Length -ne 0) { throw 'Removed credentials survived' }

        $outsideFile = Join-Path $fixture 'outside'
        [IO.File]::WriteAllText($outsideFile, 'sentinel')
        $originalAcl = (Get-Acl -LiteralPath $outsideFile).Sddl
        foreach ($linkedPath in @($sourceFile, $envFile)) {
            if ([IO.File]::Exists($linkedPath)) { [IO.File]::Delete($linkedPath) }
            New-Item -ItemType HardLink -Path $linkedPath -Target $outsideFile | Out-Null
            Invoke-Preflight -ShouldFail
            if ((Get-Acl -LiteralPath $outsideFile).Sddl -ne $originalAcl -or
                [IO.File]::ReadAllText($outsideFile) -cne 'sentinel') { throw 'Preflight modified a hardlink' }
            [IO.File]::Delete($linkedPath)
        }

        # A junction does not require Windows developer mode or symlink privileges.
        $target = Join-Path $fixture 'redirected'
        Move-Item -LiteralPath $helpers.FullName -Destination $target
        New-Item -ItemType Junction -Path $helpers.FullName -Target $target | Out-Null
        try {
            [IO.File]::WriteAllText($envFile, 'sentinel')
            Invoke-Preflight -ShouldFail
            if ([IO.File]::ReadAllText($envFile) -cne 'sentinel') { throw 'Preflight followed a junction' }
        } finally {
            [IO.Directory]::Delete($helpers.FullName)
        }
    } finally {
        Pop-Location
    }
} finally {
    if ($fixture.Parent.FullName -ne (Get-Item ([IO.Path]::GetTempPath())).FullName.TrimEnd('\') -or
        !$fixture.Name.StartsWith('NovaSharp preflight ')) { throw 'Unexpected fixture location' }
    Remove-Item -Recurse -Force -LiteralPath $fixture.FullName
}
Write-Output 'Windows host command, credential filtering and native ACL checks passed.'
