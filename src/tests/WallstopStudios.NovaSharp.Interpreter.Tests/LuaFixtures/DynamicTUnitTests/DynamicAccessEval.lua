-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/DynamicTUnitTests.cs:21
-- @test: DynamicTUnitTests.DynamicAccessEval
-- @compat-notes: NovaSharp: dynamic access; Test targets Lua 5.1
return dynamic.eval('5+1');
