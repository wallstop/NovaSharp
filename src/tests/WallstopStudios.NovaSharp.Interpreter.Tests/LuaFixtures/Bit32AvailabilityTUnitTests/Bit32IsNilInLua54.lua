-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Compatibility/Bit32AvailabilityTUnitTests.cs:104
-- @test: Bit32AvailabilityTUnitTests.Bit32IsNilInLua54
-- @compat-notes: Test targets Lua 5.3+
return bit32 == nil
