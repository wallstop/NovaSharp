-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:1325
-- @test: DebugModuleTUnitTests.GetHookReturnsNilWhenNoHookIsSet
-- @compat-notes: Test targets Lua 5.1
local func, mask, count = debug.gethook()
                return func == nil, mask == '', count == 0
