-- @lua-versions: none
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\OsTimeModuleTUnitTests.cs:377
-- @test: OsTimeModuleTUnitTests.DateSupportsEscapeAndExtendedSpecifiers
-- Test targets Lua 5.1; Lua 5.3+: bitwise OR
return os.date('!%e|%n|%t|%%|%C|%j|%u|%w', 1609459200)
