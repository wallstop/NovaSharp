-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:1066
-- @test: DebugModuleTUnitTests.SetHookWithNilFunctionClearsHook
-- @compat-notes: Lua 5.3+: bitwise operators
local function hookfn() end
                debug.sethook(hookfn, 'c', 5)
                debug.sethook(nil)
                local fn, mask, count = debug.gethook()
                return fn == nil, mask, count
