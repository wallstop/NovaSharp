-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\CoroutineModuleTUnitTests.cs:203
-- @test: CoroutineModuleTUnitTests.WrapReturnsFunctionThatResumesCoroutine
-- @compat-notes: Lua 5.3+: bitwise operators
function buildWrapper()
                    return coroutine.wrap(function()
                        for i = 1, 3 do
                            coroutine.yield(i)
                        end
                        return 'done'
                    end)
                end
