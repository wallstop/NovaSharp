-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/CoroutineTUnitTests.cs:92
-- @test: CoroutineTUnitTests.CoroutineWrapPreservesSchedulingOrder
s = ''

                function foo()
                    for i = 1, 4 do
                        s = s .. i;
                        coroutine.yield();
                    end
                end

                function bar()
                    for i = 5, 9 do
                        s = s .. i;
                        coroutine.yield();
                    end
                end

                cf = coroutine.wrap(foo);
                cb = coroutine.wrap(bar);

                for i = 1, 4 do
                    cf();
                    s = s .. '-';
                    cb();
                    s = s .. ';';
                end

                return s;
