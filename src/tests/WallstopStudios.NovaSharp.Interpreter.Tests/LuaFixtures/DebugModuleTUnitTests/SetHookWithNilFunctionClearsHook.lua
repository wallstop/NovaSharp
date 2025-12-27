-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:1743
-- @test: DebugModuleTUnitTests.SetHookWithNilFunctionClearsHook
-- @compat-notes: Test targets Lua 5.1
local function hookfn() end
                debug.sethook(hookfn, 'c', 5)
                debug.sethook(nil)
                local fn, mask, count = debug.gethook()
                return fn == nil, mask, count
