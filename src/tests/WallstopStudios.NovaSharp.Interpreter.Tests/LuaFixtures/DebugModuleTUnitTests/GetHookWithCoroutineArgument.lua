-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:1793
-- @test: DebugModuleTUnitTests.GetHookWithCoroutineArgument
-- @compat-notes: Test targets Lua 5.1
local function hookfn() end
                local co = coroutine.create(function()
                    debug.sethook(hookfn, 'l', 3)
                end)
                coroutine.resume(co)
                -- Get hook for the coroutine from outside
                local fn, mask, count = debug.gethook(co)
                return fn ~= nil, mask, count
