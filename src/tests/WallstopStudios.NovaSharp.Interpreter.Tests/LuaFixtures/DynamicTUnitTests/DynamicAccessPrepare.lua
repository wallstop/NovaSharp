-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/DynamicTUnitTests.cs:39
-- @test: DynamicTUnitTests.DynamicAccessPrepare
-- @compat-notes: NovaSharp: dynamic access; Test targets Lua 5.1
local prepared = dynamic.prepare('5+1');
                return dynamic.eval(prepared);
