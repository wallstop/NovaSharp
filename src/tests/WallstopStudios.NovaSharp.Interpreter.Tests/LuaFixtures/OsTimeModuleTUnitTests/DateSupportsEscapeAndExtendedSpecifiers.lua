-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/OsTimeModuleTUnitTests.cs:221
-- @test: OsTimeModuleTUnitTests.DateSupportsEscapeAndExtendedSpecifiers
-- @compat-notes: Lua 5.3+: bitwise OR
return os.date('!%e|%n|%t|%%|%C|%j|%u|%w', 1609459200)
