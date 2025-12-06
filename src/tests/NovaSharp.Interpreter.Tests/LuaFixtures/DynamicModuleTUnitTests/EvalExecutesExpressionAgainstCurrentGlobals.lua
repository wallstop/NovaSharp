-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Units/DynamicModuleTUnitTests.cs:20
-- @test: DynamicModuleTUnitTests.EvalExecutesExpressionAgainstCurrentGlobals
-- @compat-notes: NovaSharp: dynamic access
return dynamic.eval('value * 3')
