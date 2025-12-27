-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:164
-- @test: DebugModuleTUnitTests.GetLocalThrowsWhenLevelOutOfRange
-- @compat-notes: Test targets Lua 5.1
local ok, err = pcall(function() debug.getlocal(128, 1) end)
                return ok, err
