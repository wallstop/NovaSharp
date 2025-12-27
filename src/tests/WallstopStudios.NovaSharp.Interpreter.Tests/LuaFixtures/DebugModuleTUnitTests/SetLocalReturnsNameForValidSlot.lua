-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:141
-- @test: DebugModuleTUnitTests.SetLocalReturnsNameForValidSlot
-- @compat-notes: Test targets Lua 5.1
local assigned = debug.setlocal(0, 1, 0)
                local missing = debug.setlocal(0, 42, 0)
                return assigned, missing
