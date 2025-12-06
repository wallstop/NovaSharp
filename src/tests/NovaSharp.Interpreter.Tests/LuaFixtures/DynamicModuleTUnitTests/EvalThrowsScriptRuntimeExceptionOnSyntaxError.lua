-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: true
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Units/DynamicModuleTUnitTests.cs:69
-- @test: DynamicModuleTUnitTests.EvalThrowsScriptRuntimeExceptionOnSyntaxError
-- @compat-notes: NovaSharp: dynamic access
return dynamic.eval('function(')
