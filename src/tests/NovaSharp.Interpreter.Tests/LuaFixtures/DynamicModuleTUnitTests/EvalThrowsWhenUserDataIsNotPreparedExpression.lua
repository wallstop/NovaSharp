-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: true
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Units/DynamicModuleTUnitTests.cs:54
-- @test: DynamicModuleTUnitTests.EvalThrowsWhenUserDataIsNotPreparedExpression
-- @compat-notes: NovaSharp: dynamic access
return dynamic.eval(bad)
