-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/TableTUnitTests.cs:131
-- @test: TableTUnitTests.TableNextWithMutation
-- @compat-notes: Lua 5.3+: bitwise OR
x = {}
                function copy(k, v) x[k] = v end
                t = { a = 1, b = 2, c = 3, d = 4, e = 5 }
                k,v = next(t, nil); copy(k, v);
                k,v = next(t, k); copy(k, v); v = nil;
                k,v = next(t, k); copy(k, v);
                k,v = next(t, k); copy(k, v);
                k,v = next(t, k); copy(k, v);
                return x.a .. '|' .. x.b .. '|' .. x.c .. '|' .. x.d .. '|' .. x.e
