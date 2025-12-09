-- @lua-versions: 5.2, 5.3
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/Bit32ModuleTUnitTests.cs:710
-- @test: Bit32ModuleTUnitTests.BandAcceptsIntegralFloatLua52
-- @compat-notes: Lua 5.2/5.3 bit32.band accepts integral floats (5.0, 3.0).
-- BUG: NovaSharp times out/hangs on this - needs investigation and fix.
return bit32.band(5.0, 3.0)
