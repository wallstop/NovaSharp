-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/CoroutineModuleTUnitTests.cs:675
-- @test: CoroutineModuleTUnitTests.IsYieldableInsidePcallWithinCoroutine
-- @compat-notes: Lua 5.3+: bitwise operators
function pcallyield()
                    return coroutine.isyieldable()
                end

                function run_pcall_inside_coroutine()
                    local ok, value = pcall(pcallyield)
                    return ok, value
                end
