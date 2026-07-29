-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/DynamicTUnitTests.cs:79
-- @test: DynamicTUnitTests.DynamicAccessScopeSecurityReturnsNil
-- Compatibility notes: NovaSharp: dynamic access
a = 5;
                local prepared = dynamic.prepare('a');
                local eval = dynamic.eval;
                local _ENV = { }
                function worker()
                    local _ = _  -- Force capture of _ENV by referencing a global
                    return eval(prepared);
                end
                return worker();
