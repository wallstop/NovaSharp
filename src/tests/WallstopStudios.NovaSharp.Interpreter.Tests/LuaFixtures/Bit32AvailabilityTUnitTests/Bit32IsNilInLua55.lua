-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Compatibility/Bit32AvailabilityTUnitTests.cs:124
-- @test: Bit32AvailabilityTUnitTests.Bit32IsNilInLua55
-- @compat-notes: Test targets Lua 5.2+
return bit32 == nil
