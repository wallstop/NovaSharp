-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:1029
-- @test: DebugModuleTUnitTests.SetHookAndGetHookWithCoroutineTarget
-- @compat-notes: Lua 5.3+: bitwise operators; Uses injected variable: r
local function hookfn() end
                local co = coroutine.create(function()
                    debug.sethook(hookfn, 'r', 10)
                    local fn, mask, count = debug.gethook()
                    return fn ~= nil, mask, count
                end)
                local ok, hasFn, mask, count = coroutine.resume(co)
                return ok, hasFn, mask, count
