-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ScriptCallTUnitTests.cs:3035
-- @test: ScriptCallTUnitTests.FixedDynValueCallOverloadsPreserveTupleExpansion
-- Compatibility notes: Test targets Lua 5.1
return function(...) return select('#', ...), ... end
