-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/OsTimeModuleTUnitTests.cs:222
-- @test: OsTimeModuleTUnitTests.DateSupportsEscapeAndExtendedSpecifiers
-- @compat-notes: Test targets Lua 5.2+; Lua 5.3+: bitwise OR
return os.date('!%e|%n|%t|%%|%C|%j|%u|%w', 1609459200)
