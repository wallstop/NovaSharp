-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/DynamicTUnitTests.cs:52
-- @test: DynamicTUnitTests.DynamicAccessScope
-- @compat-notes: NovaSharp: dynamic access; Test targets Lua 5.2+
a = 3;
                local prepared = dynamic.prepare('a+1');
                function worker()
                    a = 5;
                    return dynamic.eval(prepared);
                end
                return worker();
