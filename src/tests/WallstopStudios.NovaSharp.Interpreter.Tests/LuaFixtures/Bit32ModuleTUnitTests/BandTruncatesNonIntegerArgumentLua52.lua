-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/Bit32ModuleTUnitTests.cs:703
-- @test: Bit32ModuleTUnitTests.BandTruncatesNonIntegerArgumentLua52
-- @compat-notes: Test targets Lua 5.2+; Lua 5.2+: bit32 library
return bit32.band(5.7, 3)
