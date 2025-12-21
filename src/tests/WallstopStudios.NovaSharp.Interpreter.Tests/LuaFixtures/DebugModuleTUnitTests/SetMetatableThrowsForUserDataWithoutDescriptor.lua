-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:1626
-- @test: DebugModuleTUnitTests.SetMetatableThrowsForUserDataWithoutDescriptor
-- @compat-notes: Test targets Lua 5.1
local co = coroutine.create(function() end)
                local ok, err = pcall(function() debug.setmetatable(co, {}) end)
                return ok, err
