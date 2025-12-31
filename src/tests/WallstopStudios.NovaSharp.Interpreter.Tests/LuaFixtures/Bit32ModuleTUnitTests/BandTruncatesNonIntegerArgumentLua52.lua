-- @lua-versions: 5.2
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\Bit32ModuleTUnitTests.cs:703
-- @test: Bit32ModuleTUnitTests.BandTruncatesNonIntegerArgumentLua52
-- @compat-notes: Test targets Lua 5.2+; Lua 5.2 only: bit32 library (5.2 only, removed in 5.3+)
return bit32.band(5.7, 3)
