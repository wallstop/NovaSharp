-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/CoroutineModuleTUnitTests.cs:706
-- @test: CoroutineModuleTUnitTests.WrapWithPcallHandlesTailCalls
function tail_target(...)
                    return 'tail', ...
                end

                function build_tail_wrapper()
                    return coroutine.wrap(function(...)
                        return tail_target(...)
                    end)
                end
