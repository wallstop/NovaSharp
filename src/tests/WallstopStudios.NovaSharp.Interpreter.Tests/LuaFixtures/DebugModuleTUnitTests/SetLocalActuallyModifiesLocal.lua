-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:3215
-- @test: DebugModuleTUnitTests.SetLocalActuallyModifiesLocal
-- @compat-notes: Test targets Lua 5.1
local function test()
                    local x = 'original'
                    debug.setlocal(1, 1, 'modified')
                    return x
                end
                return test()
