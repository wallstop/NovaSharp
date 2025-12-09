-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: true
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Units\CoreLib\DynamicModuleTUnitTests.cs:81
-- @test: DynamicModuleTUnitTests.PrepareThrowsScriptRuntimeExceptionOnSyntaxError
-- @compat-notes: NovaSharp: dynamic access
return dynamic.prepare('function(')
