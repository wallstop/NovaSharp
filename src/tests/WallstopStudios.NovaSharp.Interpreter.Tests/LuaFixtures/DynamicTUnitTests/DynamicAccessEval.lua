-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/DynamicTUnitTests.cs:18
-- @test: DynamicTUnitTests.DynamicAccessEval
-- @compat-notes: NovaSharp: dynamic access
return dynamic.eval('5+1');
