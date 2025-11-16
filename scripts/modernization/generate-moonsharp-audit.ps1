param()

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
    # git might be unavailable (e.g., when run from packaged artifacts)
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

$auditData = @(
    [pscustomobject]@{
        Number = 332
        Class  = 'Docs'
        Status = 'Docs backlog'
        Owner  = 'Docs'
        Note   = 'Document UnityAssetsScriptLoader usage and module path patterns in docs/UnityIntegration.md'
    }
    [pscustomobject]@{
        Number = 330
        Class  = 'Docs'
        Status = 'Docs backlog'
        Owner  = 'Docs'
        Note   = 'Publish DocFX API reference to replace dead moonsharp.org link'
    }
    [pscustomobject]@{
        Number = 328
        Class  = 'Defect'
        Status = 'Needs reproduction'
        Owner  = 'Runtime'
        Note   = 'Add test for chained RegisterAllExtensionMethods calls and fix descriptor cache invalidation'
    }
    [pscustomobject]@{
        Number = 320
        Class  = 'Legacy'
        Status = 'Superseded'
        Owner  = 'Maintainers'
        Note   = 'NovaSharp cadence documented in docs/Modernization.md; no technical action required'
    }
    [pscustomobject]@{
        Number = 319
        Class  = 'Resolved'
        Status = 'Covered'
        Owner  = 'Runtime'
        Note   = 'ParseJsonNumberValue handles unary minus tokens (JsonTableConverter.cs); add regression test in coverage push'
    }
    [pscustomobject]@{
        Number = 318
        Class  = 'Gap'
        Status = 'Backlog'
        Owner  = 'Runtime'
        Note   = 'Investigate KopiLua string.gsub pattern limits and evaluate modern regex replacement'
    }
    [pscustomobject]@{
        Number = 317
        Class  = 'Defect'
        Status = 'Needs reproduction'
        Owner  = 'Runtime'
        Note   = 'Confirm string XOR semantics against Lua 5.2 and adjust StringLib/bit32 implementation'
    }
    [pscustomobject]@{
        Number = 315
        Class  = 'Defect'
        Status = 'Queued for fix'
        Owner  = 'Runtime'
        Note   = 'Table.RawGet(int) routes key 0 through array map; patch to use value map and cover with tests'
    }
    [pscustomobject]@{
        Number = 314
        Class  = 'Docs'
        Status = 'Covered'
        Owner  = 'Docs'
        Note   = 'Modern build story uses dotnet build per docs/Testing.md; no additional work'
    }
    [pscustomobject]@{
        Number = 313
        Class  = 'Gap'
        Status = 'Needs investigation'
        Owner  = 'Debugger'
        Note   = 'Debugger stack locations missing; capture in automation harness and ensure VS Code adapter populates source info'
    }
    [pscustomobject]@{
        Number = 312
        Class  = 'Gap'
        Status = 'Backlog'
        Owner  = 'Runtime'
        Note   = 'Design richer ScriptRuntimeException payload/DecoratedMessage metadata for host diagnostics'
    }
    [pscustomobject]@{
        Number = 310
        Class  = 'Gap'
        Status = 'Needs design'
        Owner  = 'Runtime'
        Note   = 'Explore CLR callbacks that yield and resume with values; document coroutine bridging plan'
    }
    [pscustomobject]@{
        Number = 309
        Class  = 'Defect'
        Status = 'Needs reproduction'
        Owner  = 'Runtime'
        Note   = 'Reproduce goto-in-loop NullReferenceException and add VM regression coverage'
    }
    [pscustomobject]@{
        Number = 308
        Class  = 'Defect'
        Status = 'Queued for fix'
        Owner  = 'Runtime'
        Note   = 'Table.ResolveMultipleKeys throws literal {0}; pass key into ScriptRuntimeException format arguments'
    }
    [pscustomobject]@{
        Number = 307
        Class  = 'Defect'
        Status = 'Queued for fix'
        Owner  = 'Runtime'
        Note   = 'CallbackFunction.Invoke should coerce null CLR returns to DynValue.Nil before pushing onto the stack'
    }
    [pscustomobject]@{
        Number = 306
        Class  = 'Gap'
        Status = 'Backlog'
        Owner  = 'Runtime'
        Note   = 'Support table-style object initializers when constructing userdata; scope in interop roadmap'
    }
    [pscustomobject]@{
        Number = 305
        Class  = 'Defect'
        Status = 'Needs reproduction'
        Owner  = 'Runtime'
        Note   = 'ExtensionType cache behaves inconsistently; add regression around UserData.RegisterExtensionType caching'
    }
    [pscustomobject]@{
        Number = 304
        Class  = 'Gap'
        Status = 'Perf backlog'
        Owner  = 'Runtime'
        Note   = 'Quantify coroutine allocation spike (~2MB) and reduce as part of performance campaign'
    }
    [pscustomobject]@{
        Number = 302
        Class  = 'Resolved'
        Status = 'Covered'
        Owner  = 'Runtime'
        Note   = 'UnityScriptLoader on netstandard2.1 no longer touches SystemDiagnosticsSection; issue obsolete'
    }
    [pscustomobject]@{
        Number = 300
        Class  = 'Gap'
        Status = 'Tracked'
        Owner  = 'Runtime'
        Note   = 'Lua 5.3 integer parity folded into Lua 5.4 compatibility matrix (PLAN.md item 14)'
    }
    [pscustomobject]@{
        Number = 298
        Class  = 'Docs'
        Status = 'Docs backlog'
        Owner  = 'Docs'
        Note   = 'Publish HTML API reference (DocFX pipeline) to replace legacy CHM request'
    }
    [pscustomobject]@{
        Number = 297
        Class  = 'Docs'
        Status = 'Docs backlog'
        Owner  = 'Debugger'
        Note   = 'Refresh VS Code launch.json guidance to match NovaSharp debugger package layout'
    }
    [pscustomobject]@{
        Number = 296
        Class  = 'Defect'
        Status = 'Queued for fix'
        Owner  = 'Runtime'
        Note   = 'LuaBase.LuaCall nil-padding loop never increments copied; add copied++ and regression test'
    }
    [pscustomobject]@{
        Number = 294
        Class  = 'Defect'
        Status = 'Queued for fix'
        Owner  = 'Runtime'
        Note   = 'Make WEIGHT_EXACT_MATCH outrank custom converters in ScriptToClrConversions overload scoring'
    }
    [pscustomobject]@{
        Number = 293
        Class  = 'Defect'
        Status = 'Needs reproduction'
        Owner  = 'Runtime'
        Note   = 'Float/int overload heuristic still mismatches docs; add coverage for numeric overload resolution'
    }
    [pscustomobject]@{
        Number = 291
        Class  = 'Defect'
        Status = 'Needs investigation'
        Owner  = 'Runtime'
        Note   = 'DynamicMethod registration null-ref; audit UserData.RegisterType for generated methods'
    }
    [pscustomobject]@{
        Number = 288
        Class  = 'Gap'
        Status = 'Backlog'
        Owner  = 'Runtime'
        Note   = 'Improve script loaders for dotted folder names and retire outdated NuGet metadata'
    }
    [pscustomobject]@{
        Number = 287
        Class  = 'Defect'
        Status = 'Needs reproduction'
        Owner  = 'Runtime'
        Note   = 'Ensure empty tuples are distinct from nil; add DynValue tuple tests'
    }
    [pscustomobject]@{
        Number = 286
        Class  = 'Defect'
        Status = 'Needs investigation'
        Owner  = 'Runtime'
        Note   = 'SqlCommand auto-registration failure; verify reflection policy under netstandard2.1'
    }
    [pscustomobject]@{
        Number = 285
        Class  = 'Gap'
        Status = 'Queued for fix'
        Owner  = 'Runtime'
        Note   = 'Populate package table fields (path, preload, searchers) to align with Lua'
    }
    [pscustomobject]@{
        Number = 284
        Class  = 'Info'
        Status = 'No action'
        Owner  = 'Docs'
        Note   = 'Sleep/wait handled by host API; cover in scripting tips but no runtime change needed'
    }
    [pscustomobject]@{
        Number = 282
        Class  = 'Defect'
        Status = 'Needs reproduction'
        Owner  = 'Runtime'
        Note   = 'Json lexer should accept escaped forward slashes; add coverage and fix'
    }
    [pscustomobject]@{
        Number = 279
        Class  = 'Info'
        Status = 'No action'
        Owner  = 'Docs'
        Note   = 'Usage question addressed by samples; ensure docs reference function parameters as first-class values'
    }
    [pscustomobject]@{
        Number = 275
        Class  = 'Defect'
        Status = 'Queued for fix'
        Owner  = 'Runtime'
        Note   = 'Align string.format(''%s'', nil) behaviour with Lua (returns ''nil'') and guard with tests'
    }
    [pscustomobject]@{
        Number = 274
        Class  = 'Defect'
        Status = 'Needs investigation'
        Owner  = 'Runtime'
        Note   = 'Auto-registration converts CLR types incorrectly; audit UserData.AccessMode and add tests'
    }
    [pscustomobject]@{
        Number = 270
        Class  = 'Gap'
        Status = 'Backlog'
        Owner  = 'Runtime'
        Note   = 'Consider optional pretty-print flag for Json.TableToJson to support indented output'
    }
    [pscustomobject]@{
        Number = 265
        Class  = 'Legacy'
        Status = 'Won''t port'
        Owner  = 'Tooling'
        Note   = 'Legacy sync script superseded by dotnet toolchain; document new workflow only'
    }
    [pscustomobject]@{
        Number = 263
        Class  = 'Defect'
        Status = 'Needs reproduction'
        Owner  = 'Runtime'
        Note   = 'Unassigned locals retaining stale values; cover in VM tests and fix environment handling'
    }
    [pscustomobject]@{
        Number = 261
        Class  = 'Defect'
        Status = 'Queued for fix'
        Owner  = 'Debugger'
        Note   = 'Signal(Byte|Source)CodeChange must honour GetDebuggerCaps() in remote debugger transport'
    }
    [pscustomobject]@{
        Number = 258
        Class  = 'Defect'
        Status = 'Needs reproduction'
        Owner  = 'Runtime'
        Note   = 'lua type on void should yield ''nil'' not error; review TypeValidationFlags handling'
    }
    [pscustomobject]@{
        Number = 257
        Class  = 'Defect'
        Status = 'Queued for fix'
        Owner  = 'Runtime'
        Note   = 'Json parser fails on nested arrays; adjust lexer/token advance logic'
    }
    [pscustomobject]@{
        Number = 255
        Class  = 'Defect'
        Status = 'Needs reproduction'
        Owner  = 'Tooling'
        Note   = 'Hardwire generator must skip implicit operators under Unity; add filter and regression tests'
    }
    [pscustomobject]@{
        Number = 253
        Class  = 'Docs'
        Status = 'Docs backlog'
        Owner  = 'Docs'
        Note   = 'Add troubleshooting section for returning custom userdata and registering factories'
    }
    [pscustomobject]@{
        Number = 245
        Class  = 'Gap'
        Status = 'Tracked'
        Owner  = 'Runtime'
        Note   = 'Implement __name metaproperty during Lua 5.4 parity work'
    }
    [pscustomobject]@{
        Number = 244
        Class  = 'Gap'
        Status = 'Backlog'
        Owner  = 'Debugger'
        Note   = 'Source-map aware debugging requested; evaluate once orchestration library lands'
    }
    [pscustomobject]@{
        Number = 243
        Class  = 'Gap'
        Status = 'Backlog'
        Owner  = 'Debugger'
        Note   = 'Design multi-script debug session support and track in debugger orchestration roadmap'
    }
    [pscustomobject]@{
        Number = 241
        Class  = 'Defect'
        Status = 'Needs investigation'
        Owner  = 'Runtime'
        Note   = 'Reproduce bytecode loading error from legacy report and validate dump/load path'
    }
    [pscustomobject]@{
        Number = 240
        Class  = 'Gap'
        Status = 'Backlog'
        Owner  = 'Runtime'
        Note   = 'Expose debug module in Unity builds via safe feature toggle'
    }
    [pscustomobject]@{
        Number = 236
        Class  = 'Defect'
        Status = 'Needs reproduction'
        Owner  = 'Runtime'
        Note   = '__newindex implementation mismatch; add metamethod tests and correct behaviour'
    }
    [pscustomobject]@{
        Number = 235
        Class  = 'Resolved'
        Status = 'Covered'
        Owner  = 'Runtime'
        Note   = 'UnityAssetsScriptLoaderTests verify .lua asset imports; Unity support restored'
    }
    [pscustomobject]@{
        Number = 232
        Class  = 'Defect'
        Status = 'Needs reproduction'
        Owner  = 'Runtime'
        Note   = 'Intermittent IndexOutOfRange in Closure.Call; add stress tests around nested asserts'
    }
    [pscustomobject]@{
        Number = 231
        Class  = 'Gap'
        Status = 'Backlog'
        Owner  = 'Runtime'
        Note   = 'Introduce script cancellation/timeouts via cooperative checks and host tokens'
    }
    [pscustomobject]@{
        Number = 229
        Class  = 'Defect'
        Status = 'Queued for fix'
        Owner  = 'Runtime'
        Note   = 'tonumber("0x23") should parse hex; update StringModule numeric parsing and add tests'
    }
    [pscustomobject]@{
        Number = 228
        Class  = 'Gap'
        Status = 'Backlog'
        Owner  = 'Runtime'
        Note   = 'Plan async/await integration by bridging CLR tasks to Lua coroutines'
    }
    [pscustomobject]@{
        Number = 221
        Class  = 'Defect'
        Status = 'Backlog'
        Owner  = 'Runtime'
        Note   = 'Add stack overflow guards and configurable recursion depth to Script/Processor'
    }
    [pscustomobject]@{
        Number = 219
        Class  = 'Defect'
        Status = 'Queued for fix'
        Owner  = 'Runtime'
        Note   = 'json.parse 2D arrays currently fail; share lexer fix with issue #257'
    }
    [pscustomobject]@{
        Number = 218
        Class  = 'Defect'
        Status = 'Needs reproduction'
        Owner  = 'Runtime'
        Note   = 'Nested Assert wrapper triggers AssignLocal IndexOutOfRange; create coroutine regression test'
    }
    [pscustomobject]@{
        Number = 213
        Class  = 'Gap'
        Status = 'Won''t implement'
        Owner  = 'Runtime'
        Note   = 'Maintain case-sensitive userdata members; document naming guidance instead'
    }
    [pscustomobject]@{
        Number = 212
        Class  = 'Legacy'
        Status = 'Won''t port'
        Owner  = 'Runtime'
        Note   = 'Unity 2017 WebGL unsupported; NovaSharp targets Unity 2021+'
    }
    [pscustomobject]@{
        Number = 210
        Class  = 'Docs'
        Status = 'Covered'
        Owner  = 'Docs'
        Note   = 'New documentation reflects BackgroundOptimized vs Preoptimized correctly'
    }
    [pscustomobject]@{
        Number = 209
        Class  = 'Defect'
        Status = 'Needs reproduction'
        Owner  = 'Runtime'
        Note   = 'UserData.RegisterAssembly should work on netstandard2.1; add regression coverage'
    }
    [pscustomobject]@{
        Number = 208
        Class  = 'Docs'
        Status = 'Docs backlog'
        Owner  = 'Docs'
        Note   = 'Document exposing constructors via UserData.CreateStatic and RegisterType'
    }
    [pscustomobject]@{
        Number = 207
        Class  = 'Docs'
        Status = 'Docs backlog'
        Owner  = 'Docs'
        Note   = 'Provide example for organizing script modules with global access (Forge-style)'
    }
    [pscustomobject]@{
        Number = 204
        Class  = 'Defect'
        Status = 'Queued for fix'
        Owner  = 'Runtime'
        Note   = 'json.parse/json.serialize should round-trip empty arrays; adjust serializer logic'
    }
    [pscustomobject]@{
        Number = 199
        Class  = 'Defect'
        Status = 'Needs reproduction'
        Owner  = 'Runtime'
        Note   = 'Table.Get should honour __index metamethod; add coverage and patch'
    }
    [pscustomobject]@{
        Number = 198
        Class  = 'Defect'
        Status = 'Queued for fix'
        Owner  = 'Runtime'
        Note   = 'Varargs no-argument call path broken; repair tuple handling in VM'
    }
    [pscustomobject]@{
        Number = 197
        Class  = 'Gap'
        Status = 'Backlog'
        Owner  = 'Debugger'
        Note   = 'Investigate VS Code debugger regression post 1.13 and align NovaSharp extension docs'
    }
    [pscustomobject]@{
        Number = 196
        Class  = 'Legacy'
        Status = 'Won''t port'
        Owner  = 'Runtime'
        Note   = 'Portable profile 111 dropped; modernization targets netstandard2.1/net8.0'
    }
    [pscustomobject]@{
        Number = 192
        Class  = 'Defect'
        Status = 'Needs reproduction'
        Owner  = 'Tooling'
        Note   = 'NovaSharp.Cli should treat Ctrl+Z as EOF; add console integration test'
    }
    [pscustomobject]@{
        Number = 190
        Class  = 'Defect'
        Status = 'Needs investigation'
        Owner  = 'Runtime'
        Note   = 'Optional/params overload binding misbehaves; expand coverage in interop tests'
    }
    [pscustomobject]@{
        Number = 187
        Class  = 'Defect'
        Status = 'Needs reproduction'
        Owner  = 'Runtime'
        Note   = 'Unicode gsub replacement bug; add StringLib Unicode tests and adjust KopiLuaStringLib'
    }
    [pscustomobject]@{
        Number = 186
        Class  = 'Defect'
        Status = 'Queued for fix'
        Owner  = 'Runtime'
        Note   = 'Allow Lua closures to convert to System.Action by wrapping or auto-binding signatures'
    }
    [pscustomobject]@{
        Number = 185
        Class  = 'Gap'
        Status = 'Backlog'
        Owner  = 'Runtime'
        Note   = 'Reintroduce loadstring alias delegating to load() for compatibility'
    }
    [pscustomobject]@{
        Number = 183
        Class  = 'Gap'
        Status = 'Backlog'
        Owner  = 'Runtime'
        Note   = 'Support operator overloads on proxy userdata; adjust descriptor pipeline'
    }
    [pscustomobject]@{
        Number = 181
        Class  = 'Gap'
        Status = 'Backlog'
        Owner  = 'Runtime'
        Note   = 'Enable per-script TypeDescriptorRegistry/ExtensionMethodsRegistry injection'
    }
    [pscustomobject]@{
        Number = 180
        Class  = 'Defect'
        Status = 'Needs reproduction'
        Owner  = 'Runtime'
        Note   = 'JsonTableConverter.JsonToTable should handle escaped forward slash; align with issue #282 fix'
    }
    [pscustomobject]@{
        Number = 175
        Class  = 'Defect'
        Status = 'Queued for fix'
        Owner  = 'Runtime'
        Note   = 'Port upstream PR #336 (moonsharp-devs/moonsharp) to restore static inheritance support in userdata'
    }
    [pscustomobject]@{
        Number = 169
        Class  = 'Legacy'
        Status = 'Won''t port'
        Owner  = 'Runtime'
        Note   = 'WinRT support dropped during modernization; no action'
    }
    [pscustomobject]@{
        Number = 161
        Class  = 'Resolved'
        Status = 'Covered'
        Owner  = 'Runtime'
        Note   = 'IoModuleVirtualizationTests confirm io.write/io.output piping to configured streams'
    }
)

# Build lookup
$auditLookup = @{}
foreach ($item in $auditData) {
    $auditLookup[$item.Number] = $item
}

$header = @(
    '# Legacy Issue Audit (NovaSharp Port)',
    '',
    ('Generated: {0}' -f (Get-Date -Format 'yyyy-MM-dd')),
    '',
    '| Issue | Title | Classification | Status | Owner | Notes |',
    '|-------|-------|----------------|--------|-------|-------|'
)

$headers = @{
    'User-Agent' = 'NovaSharpAudit'
}
$issueUri = 'https://api.github.com/repos/moonsharp-devs/moonsharp/issues?state=open&per_page=100'
$issues = Invoke-RestMethod -Headers $headers -Uri $issueUri
$issues = $issues | Where-Object { $_.pull_request -eq $null } | Sort-Object number -Descending

$output = @()
$output += $header

foreach ($issue in $issues) {
    $number = [int]$issue.number
    if (-not $auditLookup.ContainsKey($number)) {
        throw "Missing audit entry for issue #$number"
    }
    $entry = $auditLookup[$number]
    $safeTitle = ($issue.title -replace '\|', '-')
    $formatted = "| [#$number]($($issue.html_url)) | $safeTitle | $($entry.Class) | $($entry.Status) | $($entry.Owner) | $($entry.Note) |"
    $output += $formatted
}

$outputPath = Join-Path $repoRoot "docs/modernization/moonsharp-issue-audit.md"
$output | Set-Content $outputPath -Encoding UTF8
Write-Host ("Legacy issue audit regenerated at {0}" -f $outputPath)
