-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/CoreLib/DynamicModuleTUnitTests.cs:65
-- @test: DynamicModuleTUnitTests.EvalThrowsWhenUserDataIsNotPreparedExpression
-- @compat-notes: NovaSharp: dynamic access
return dynamic.eval(bad)
