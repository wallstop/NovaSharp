-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: true
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Units/DynamicModuleTUnitTests.cs:81
-- @test: DynamicModuleTUnitTests.PrepareThrowsScriptRuntimeExceptionOnSyntaxError
-- @compat-notes: NovaSharp: dynamic access
return dynamic.prepare('function(')
