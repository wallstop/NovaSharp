-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/VM/ProcessorCoroutineModuleTUnitTests.cs:119
-- @test: ProcessorCoroutineModuleTUnitTests.StatusReturnsNormalWhenInspectingMainFromChild
-- @compat-notes: Lua 5.3+: bitwise operators
local mainCoroutine = select(1, coroutine.running())

                function queryMainStatus()
                    return coroutine.status(mainCoroutine)
                end
