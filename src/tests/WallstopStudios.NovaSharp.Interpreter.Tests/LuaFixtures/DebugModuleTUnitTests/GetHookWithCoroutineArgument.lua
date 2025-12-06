-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:1106
-- @test: DebugModuleTUnitTests.GetHookWithCoroutineArgument
-- @compat-notes: Lua 5.3+: bitwise operators
local function hookfn() end
                local co = coroutine.create(function()
                    debug.sethook(hookfn, 'l', 3)
                end)
                coroutine.resume(co)
                -- Get hook for the coroutine from outside
                local fn, mask, count = debug.gethook(co)
                return fn ~= nil, mask, count
