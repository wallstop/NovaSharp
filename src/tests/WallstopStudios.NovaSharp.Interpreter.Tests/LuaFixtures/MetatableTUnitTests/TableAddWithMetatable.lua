-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/MetatableTUnitTests.cs:70
-- @test: MetatableTUnitTests.TableAddWithMetatable
v1 = { 'aaaa' }
                v2 = { 'aaaaaa' }
                meta = { }
                function meta.__add(t1, t2)
                    local o1 = #t1[1];
                    local o2 = #t2[1];
                    return o1 * o2;
                end
                setmetatable(v1, meta);
                return v1 + v2;
