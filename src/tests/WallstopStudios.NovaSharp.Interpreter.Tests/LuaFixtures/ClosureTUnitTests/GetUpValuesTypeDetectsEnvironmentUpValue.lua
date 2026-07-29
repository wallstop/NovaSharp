-- @lua-versions: 5.2+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Units\DataTypes\ClosureTUnitTests.cs:59
-- @test: ClosureTUnitTests.GetUpValuesTypeDetectsEnvironmentUpValue
-- Lua 5.2+: _ENV variable
return function() return _ENV end
