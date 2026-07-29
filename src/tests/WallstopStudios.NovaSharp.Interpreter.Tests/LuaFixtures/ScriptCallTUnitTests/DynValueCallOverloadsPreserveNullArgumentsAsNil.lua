-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ScriptCallTUnitTests.cs:2910
-- @test: ScriptCallTUnitTests.DynValueCallOverloadsPreserveNullArgumentsAsNil
-- Compatibility notes: Test targets Lua 5.1
return function(...) return select('#', ...), ... end
