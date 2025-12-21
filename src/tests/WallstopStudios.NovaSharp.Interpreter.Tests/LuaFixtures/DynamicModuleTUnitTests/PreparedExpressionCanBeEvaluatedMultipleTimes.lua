-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/CoreLib/DynamicModuleTUnitTests.cs:39
-- @test: DynamicModuleTUnitTests.PreparedExpressionCanBeEvaluatedMultipleTimes
-- @compat-notes: NovaSharp: dynamic access
return dynamic.prepare('a + b')
