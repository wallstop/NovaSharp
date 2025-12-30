-- @lua-versions: 5.2
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/Bit32ModuleTUnitTests.cs:715
-- @test: Bit32ModuleTUnitTests.BandAcceptsIntegralFloatLua52
-- @compat-notes: bit32 library only available by default in Lua 5.2 (not bundled in 5.3+)
return bit32.band(5.0, 3.0)