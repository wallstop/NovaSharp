-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/DataTypes/ClosureTUnitTests.cs:50
-- @test: ClosureTUnitTests.GetUpValuesTypeDetectsEnvironmentUpValue
-- @compat-notes: Lua 5.2+: _ENV variable
return function() return _ENV end
