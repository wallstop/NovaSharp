-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/MetatableTUnitTests.cs:134
-- @test: MetatableTUnitTests.MetatableCallUpdatesState
t = {}
                meta = {}
                x = 0;
                function meta.__call(f, y)
                    x = 156 * y;
                end
                setmetatable(t, meta);
                t(3);
                return x;
