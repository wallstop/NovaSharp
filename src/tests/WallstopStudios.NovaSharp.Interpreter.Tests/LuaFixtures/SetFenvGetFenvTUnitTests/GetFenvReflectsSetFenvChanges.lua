-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/SetFenvGetFenvTUnitTests.cs:171
-- @test: SetFenvGetFenvTUnitTests.GetFenvReflectsSetFenvChanges
-- @compat-notes: Test targets Lua 5.1
local function f() return 1 end
                local env = { custom = true }
                setmetatable(env, { __index = _G })
                setfenv(f, env)
                return getfenv(f).custom
