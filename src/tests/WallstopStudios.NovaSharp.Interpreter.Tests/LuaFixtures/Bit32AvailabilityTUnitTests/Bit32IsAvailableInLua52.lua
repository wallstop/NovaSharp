-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Compatibility/Bit32AvailabilityTUnitTests.cs:64
-- @test: Bit32AvailabilityTUnitTests.Bit32IsAvailableInLua52
-- @compat-notes: Test targets Lua 5.1
return type(bit32) == 'table'
