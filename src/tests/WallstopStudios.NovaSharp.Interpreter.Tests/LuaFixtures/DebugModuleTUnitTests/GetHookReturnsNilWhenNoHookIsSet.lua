-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:734
-- @test: DebugModuleTUnitTests.GetHookReturnsNilWhenNoHookIsSet
-- @compat-notes: Lua 5.3+: bitwise operators
local func, mask, count = debug.gethook()
                return func == nil, mask == '', count == 0
