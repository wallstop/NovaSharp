-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ScriptCallTUnitTests.cs:2452
-- @test: ScriptCallTUnitTests.CallWithReadOnlySpanDynValuesInvokesLuaFunction
return function(a, b, c, d, e) return a + b + c + d + e end
