-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:813
-- @test: DebugModuleTUnitTests.SetMetatableThrowsForNonTableMetatable
-- @compat-notes: Test targets Lua 5.1
local ok, err = pcall(function() debug.setmetatable({}, 'notatable') end)
                return ok, err
