-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:211
-- @test: DebugModuleTUnitTests.SetLocalReportsLevelOutOfRange
-- @compat-notes: Test targets Lua 5.1
local ok, err = pcall(function() debug.setlocal(42, 1, true) end)
                return ok, err
