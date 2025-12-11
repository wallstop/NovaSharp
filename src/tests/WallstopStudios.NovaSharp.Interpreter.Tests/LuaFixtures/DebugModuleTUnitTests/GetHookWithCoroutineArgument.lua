-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\DebugModuleTUnitTests.cs:1119
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
