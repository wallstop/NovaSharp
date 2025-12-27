-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/CoroutineTUnitTests.cs:260
-- @test: CoroutineTUnitTests.CoroutineAutoYieldResumesUntilCompletion
function fib(n)
                    if (n == 0 or n == 1) then
                        return 1;
                    else
                        return fib(n - 1) + fib(n - 2);
                    end
                end
