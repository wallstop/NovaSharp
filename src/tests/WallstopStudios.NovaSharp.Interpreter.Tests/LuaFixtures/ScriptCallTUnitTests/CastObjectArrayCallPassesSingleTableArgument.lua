-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ScriptCallTUnitTests.cs:2990
-- @test: ScriptCallTUnitTests.CastObjectArrayCallPassesSingleTableArgument
-- Compatibility notes: Test targets Lua 5.1
return function(value) return type(value), #value, value[1], value[2] end
