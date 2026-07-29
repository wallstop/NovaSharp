-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/DataTypes/ClosureTUnitTests.cs:24
-- @test: ClosureTUnitTests.ClosureWithNoGlobalsHasNoEnvUpvalue
-- Compatibility notes: Test targets Lua 5.1
return function(a) return a end
