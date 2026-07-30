-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ScriptExecution/ScriptCallTUnitTests.cs:187
-- @test: ScriptCallTUnitTests.CallObjectArgumentsSupportsCallerOwnedSpanAndObjectFunction
-- Compatibility notes: Test targets Lua 5.2+
function capture(...) return select('#', ...), ... end
