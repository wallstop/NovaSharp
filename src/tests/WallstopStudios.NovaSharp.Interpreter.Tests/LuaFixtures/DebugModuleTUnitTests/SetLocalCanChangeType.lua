-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:3244
-- @test: DebugModuleTUnitTests.SetLocalCanChangeType
-- @compat-notes: Test targets Lua 5.1
local function test()
                    local x = 42  -- number
                    debug.setlocal(1, 1, 'now a string')
                    return type(x), x
                end
                return test()
