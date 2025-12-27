-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Compatibility/Bit32AvailabilityTUnitTests.cs:44
-- @test: Bit32AvailabilityTUnitTests.Bit32IsNilInLua51
-- @compat-notes: Test targets Lua 5.1
return bit32 == nil
