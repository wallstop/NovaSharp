-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Units/DynamicModuleTUnitTests.cs:31
-- @test: DynamicModuleTUnitTests.PreparedExpressionCanBeEvaluatedMultipleTimes
-- @compat-notes: NovaSharp: dynamic access
return dynamic.prepare('a + b')
