-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: true
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Units\CoreLib\DynamicModuleTUnitTests.cs:83
-- @test: DynamicModuleTUnitTests.EvalThrowsScriptRuntimeExceptionOnSyntaxError
-- NovaSharp: dynamic access
return dynamic.eval('function(')
