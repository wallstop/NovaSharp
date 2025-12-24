-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/SetFenvGetFenvTUnitTests.cs:279
-- @test: SetFenvGetFenvTUnitTests.SetFenvWithLevelChangesEnvironment
-- @compat-notes: Test targets Lua 5.1
local result = nil
                local function test_level()
                    local new_env = { custom_value = 123 }
                    setmetatable(new_env, { __index = _G })
                    setfenv(1, new_env)
                    result = custom_value
                end
                test_level()
                return result
