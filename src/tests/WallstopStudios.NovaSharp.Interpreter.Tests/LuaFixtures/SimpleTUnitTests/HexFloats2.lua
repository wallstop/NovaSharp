-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/SimpleTUnitTests.cs:1675
-- @test: SimpleTUnitTests.HexFloats2
-- @compat-notes: Lua 5.2+: hex float with exponent (5.2+)
return 0xA23p-4
