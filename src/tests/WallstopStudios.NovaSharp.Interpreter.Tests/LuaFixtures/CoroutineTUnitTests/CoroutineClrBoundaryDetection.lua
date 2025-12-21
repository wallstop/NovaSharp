-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/CoroutineTUnitTests.cs:119
-- @test: CoroutineTUnitTests.CoroutineClrBoundaryDetection
-- @compat-notes: Uses injected variable: callback
function a()
                    callback(b)
                end

                function b()
                    coroutine.yield();
                end

                c = coroutine.create(a);
                return coroutine.resume(c);
