-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/MetatableTUnitTests.cs:161
-- @test: MetatableTUnitTests.MetatableIndexAndSetIndexFunctions
-- @compat-notes: Uses injected variable: obj
T = { a = 'a', b = 'b', c = 'c' };
                t = { };
                m = { };
                s = '';
                function m.__index(obj, idx)
                    return T[idx];
                end
                function m.__newindex(obj, idx, val)
                    T[idx] = val;
                end
                setmetatable(t, m);
                s = s .. t.a .. t.b .. t.c;
                t.a = '!';
                s = s .. t.a .. t.b .. t.c;
                return s;
