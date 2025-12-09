-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\DebugModuleTUnitTests.cs:140
-- @test: DebugModuleTUnitTests.SetHookRecordsMaskAndCount
-- @compat-notes: Lua 5.3+: bitwise operators
local function hook() end
                debug.sethook(hook, 'c', 42)
                local fn, mask, count = debug.gethook()
                return fn ~= nil, mask, count
