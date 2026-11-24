# Windows PowerShell 5.1 host helper. Bash supplies only the validated Docker
# env-file bytes and a NUL completion marker on stdin. NTFS security is applied
# before any bytes are written.
param(
    [Parameter(Mandatory)][string] $Workspace,
    [Parameter(Mandatory)][ValidateSet('Protect', 'Publish')][string] $Action
)
$ErrorActionPreference = 'Stop'
$identity = [Security.Principal.WindowsIdentity]::GetCurrent()
$userSid = $identity.User
function New-PrivateAcl([bool] $SetOwner) {
    $acl = [Security.AccessControl.FileSecurity]::new()
    if ($SetOwner) { $acl.SetOwner($userSid) }
    $acl.SetAccessRuleProtection($true, $false)
    $acl.AddAccessRule([Security.AccessControl.FileSystemAccessRule]::new(
        $userSid, 'FullControl', 'Allow'))
    return $acl
}

function Test-RegularPath([string] $Path, [bool] $Directory) {
    try {
        $attributes = [IO.File]::GetAttributes($Path)
    } catch [IO.FileNotFoundException] {
        return $false
    }
    if (($attributes -band [IO.FileAttributes]::ReparsePoint) -or
        ([bool]($attributes -band [IO.FileAttributes]::Directory) -ne $Directory)) {
        throw "Refusing redirected or unexpected MCP credential path: $Path"
    }
    if (-not $Directory -and (Get-Item -LiteralPath $Path -Force).LinkType -eq 'HardLink') {
        throw "Refusing a multiply-linked credential file: $Path"
    }
    return $true
}

function Assert-PrivateAcl([Security.AccessControl.FileSecurity] $Acl) {
    $rules = $Acl.GetAccessRules($true, $true, [Security.Principal.SecurityIdentifier])
    if ($Acl.GetOwner([Security.Principal.SecurityIdentifier]) -ne $userSid -or
        -not $Acl.AreAccessRulesProtected -or $rules.Count -ne 1 -or
        $rules[0].IdentityReference -ne $userSid -or
        $rules[0].AccessControlType -ne 'Allow' -or $rules[0].FileSystemRights -ne 'FullControl') {
        throw 'NTFS did not enforce a private, current-user-only credential ACL.'
    }
}

function Protect-ExistingFile([string] $Path) {
    if (Test-RegularPath $Path $false) {
        $owner = [IO.File]::GetAccessControl($Path).GetOwner([Security.Principal.SecurityIdentifier])
        if ($owner -ne $userSid -and $owner -ne $identity.Owner) {
            throw "Credential file must belong to the current host user: $Path"
        }
        [IO.File]::SetAccessControl($Path, (New-PrivateAcl ($owner -ne $userSid)))
        Assert-PrivateAcl ([IO.File]::GetAccessControl($Path))
    }
}

# Windows link/ACL queries can briefly deny rename sharing. Keep these queries
# and the rename mutually exclusive for simultaneous preflights of this checkout.
function Invoke-WithCredentialLock([scriptblock] $Operation) {
    $hasher = [Security.Cryptography.SHA256]::Create()
    try {
        $pathBytes = [Text.Encoding]::UTF8.GetBytes([IO.Path]::GetFullPath($Workspace).TrimEnd('\').ToUpperInvariant())
        $name = 'Local\NovaSharp.McpEnv.' + [BitConverter]::ToString($hasher.ComputeHash($pathBytes))
    } finally {
        $hasher.Dispose()
    }
    $mutex = [Threading.Mutex]::new($false, $name)
    try {
        try { $null = $mutex.WaitOne() } catch [Threading.AbandonedMutexException] {
            # The previous owner exited. WaitOne granted us ownership.
        }
        try { & $Operation } finally { $mutex.ReleaseMutex() }
    } finally {
        $mutex.Dispose()
    }
}

$directory = [IO.Path]::Combine($Workspace, '.devcontainer')
if (-not (Test-RegularPath $Workspace $true) -or -not (Test-RegularPath $directory $true)) {
    throw 'Workspace and .devcontainer must be real directories.'
}
$destination = [IO.Path]::Combine($directory, '.mcp.env')
if ($Action -eq 'Protect') {
    Invoke-WithCredentialLock {
        Protect-ExistingFile $destination
        Protect-ExistingFile ([IO.Path]::Combine($Workspace, '.env.local'))
    }
    return
}

# File.Replace inherits the old destination ACL. A same-volume Windows rename
# preserves the verified ACL of the new file and works for both first publish
# and concurrent replacement.
Add-Type -Namespace NovaSharp -Name NativeFile -MemberDefinition @'
[System.Runtime.InteropServices.DllImport("kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true)]
public static extern bool MoveFileEx(string existing, string replacement, uint flags);
'@
$temporary = "$destination.$([Guid]::NewGuid().ToString('N'))"
$stream = [IO.FileStream]::new($temporary, [IO.FileMode]::CreateNew,
    [Security.AccessControl.FileSystemRights]::FullControl, [IO.FileShare]::None,
    4096, [IO.FileOptions]::None, (New-PrivateAcl $true))
try {
    try {
        Assert-PrivateAcl ($stream.GetAccessControl())
        [Console]::OpenStandardInput().CopyTo($stream)
        if ($stream.Length -eq 0) { throw 'Incomplete credential input.' }
        $stream.Position = $stream.Length - 1
        if ($stream.ReadByte() -ne 0) { throw 'Incomplete credential input.' }
        $stream.SetLength($stream.Length - 1)
    } finally {
        $stream.Dispose()
    }
    Invoke-WithCredentialLock {
        $null = Test-RegularPath $destination $false
        if (-not [NovaSharp.NativeFile]::MoveFileEx($temporary, $destination, 1)) {
            throw [ComponentModel.Win32Exception]::new([Runtime.InteropServices.Marshal]::GetLastWin32Error())
        }
        if (-not (Test-RegularPath $destination $false)) { throw 'The published credential file is missing.' }
        Assert-PrivateAcl ([IO.File]::GetAccessControl($destination))
    }
} finally {
    [IO.File]::Delete($temporary)
}
