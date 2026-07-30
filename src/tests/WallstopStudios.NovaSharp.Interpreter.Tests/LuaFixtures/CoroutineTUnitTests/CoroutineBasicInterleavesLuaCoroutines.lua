-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/CoroutineTUnitTests.cs:50
-- @test: CoroutineTUnitTests.CoroutineBasicInterleavesLuaCoroutines
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

                cf = coroutine.create(foo);
                cb = coroutine.create(bar);

                for i = 1, 4 do
                    coroutine.resume(cf);
                    s = s .. '-';
                    coroutine.resume(cb);
                    s = s .. ';';
                end

                return s;
