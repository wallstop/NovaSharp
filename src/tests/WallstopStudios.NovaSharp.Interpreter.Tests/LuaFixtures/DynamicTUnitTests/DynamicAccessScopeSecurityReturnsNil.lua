-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/DynamicTUnitTests.cs:74
-- @test: DynamicTUnitTests.DynamicAccessScopeSecurityReturnsNil
-- @compat-notes: NovaSharp: dynamic access; Test targets Lua 5.2+
a = 5;
                local prepared = dynamic.prepare('a');
                local eval = dynamic.eval;
                local _ENV = { }
                function worker()
                    return eval(prepared);
                end
                return worker();
