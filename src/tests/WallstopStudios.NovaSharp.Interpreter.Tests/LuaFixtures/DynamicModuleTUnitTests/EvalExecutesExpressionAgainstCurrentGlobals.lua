-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/CoreLib/DynamicModuleTUnitTests.cs:25
-- @test: DynamicModuleTUnitTests.EvalExecutesExpressionAgainstCurrentGlobals
-- @compat-notes: NovaSharp: dynamic access
return dynamic.eval('value * 3')
