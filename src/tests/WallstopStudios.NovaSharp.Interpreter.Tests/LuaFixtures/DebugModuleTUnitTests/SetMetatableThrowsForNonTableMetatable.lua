-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:615
-- @test: DebugModuleTUnitTests.SetMetatableThrowsForNonTableMetatable
-- @compat-notes: Test targets Lua 5.1
local ok, err = pcall(function() debug.setmetatable({}, 'notatable') end)
                return ok, err
