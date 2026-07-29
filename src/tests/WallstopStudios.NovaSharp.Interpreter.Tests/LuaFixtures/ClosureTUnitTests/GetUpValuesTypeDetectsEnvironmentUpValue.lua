-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/DataTypes/ClosureTUnitTests.cs:72
-- @test: ClosureTUnitTests.GetUpValuesTypeDetectsEnvironmentUpValue
-- Compatibility notes: Lua 5.2+: _ENV variable
return function() return _ENV end
