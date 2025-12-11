-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\DebugModuleTUnitTests.cs:765
-- @test: DebugModuleTUnitTests.SetHookClearsHookWhenNilPassed
-- @compat-notes: Lua 5.3+: bitwise operators
local called = false
                debug.sethook(function() called = true end, 'l')
                debug.sethook(nil)
                local f, m, c = debug.gethook()
                return f == nil, m == ''
