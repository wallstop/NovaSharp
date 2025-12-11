-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\Bit32ModuleTUnitTests.cs:733
-- @test: Bit32ModuleTUnitTests.LshiftTruncatesNonIntegerShiftAmountLua52
-- @compat-notes: Test targets Lua 5.2+; Lua 5.2+: bit32 library
return bit32.lshift(1, 2.5)
