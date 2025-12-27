-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/MetatableTUnitTests.cs:112
-- @test: MetatableTUnitTests.MetatableCallReturnsValue
t = {}
                meta = {}
                function meta.__call(f, y)
                    return 156 * y
                end
                setmetatable(t, meta);
                return t;
