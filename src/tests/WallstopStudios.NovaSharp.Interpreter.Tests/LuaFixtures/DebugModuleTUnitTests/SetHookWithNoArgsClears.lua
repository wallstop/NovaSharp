-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:1489
-- @test: DebugModuleTUnitTests.SetHookWithNoArgsClears
-- @compat-notes: Test targets Lua 5.1
local function hookfn() end
                debug.sethook(hookfn, 'c', 5)
                local fn1, mask1, count1 = debug.gethook()
                debug.sethook() -- Call with no args to clear
                local fn2, mask2, count2 = debug.gethook()
                return fn1 ~= nil, fn2 == nil, mask2, count2
