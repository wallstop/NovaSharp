-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/VM/ProcessorCoroutineModuleTUnitTests.cs:187
-- @test: ProcessorCoroutineModuleTUnitTests.WrapReturnsFunctionThatResumesCoroutine
-- @compat-notes: Lua 5.3+: bitwise operators
function buildWrapper()
                    return coroutine.wrap(function()
                        for i = 1, 3 do
                            coroutine.yield(i)
                        end
                        return 'done'
                    end)
                end
