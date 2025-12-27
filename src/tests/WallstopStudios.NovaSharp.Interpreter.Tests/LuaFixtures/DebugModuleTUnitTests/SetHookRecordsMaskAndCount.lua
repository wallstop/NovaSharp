-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:186
-- @test: DebugModuleTUnitTests.SetHookRecordsMaskAndCount
-- @compat-notes: Test targets Lua 5.1
local function hook() end
                debug.sethook(hook, 'c', 42)
                local fn, mask, count = debug.gethook()
                return fn ~= nil, mask, count
