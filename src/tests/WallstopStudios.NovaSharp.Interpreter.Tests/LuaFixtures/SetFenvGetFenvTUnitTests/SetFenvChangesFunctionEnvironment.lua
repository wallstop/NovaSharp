-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/SetFenvGetFenvTUnitTests.cs:135
-- @test: SetFenvGetFenvTUnitTests.SetFenvChangesFunctionEnvironment
-- @compat-notes: Test targets Lua 5.1
local function f() return x end
                local env = { x = 42 }
                setmetatable(env, { __index = _G })
                setfenv(f, env)
                return f()
