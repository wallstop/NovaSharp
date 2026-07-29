-- @lua-versions: 5.2
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\Bit32ModuleTUnitTests.cs:742
-- @test: Bit32ModuleTUnitTests.LshiftTruncatesNonIntegerShiftAmountLua52
-- Test targets Lua 5.2+; Lua 5.2 only: bit32 library (5.2 only, removed in 5.3+)
return bit32.lshift(1, 2.5)
