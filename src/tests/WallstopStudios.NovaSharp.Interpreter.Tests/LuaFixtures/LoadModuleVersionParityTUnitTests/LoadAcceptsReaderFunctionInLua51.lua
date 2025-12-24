-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/LoadModuleVersionParityTUnitTests.cs:134
-- @test: LoadModuleVersionParityTUnitTests.LoadAcceptsReaderFunctionInLua51
-- @compat-notes: Test targets Lua 5.1
local done = false
                local function reader()
                    if done then return nil end
                    done = true
                    return 'return 123'
                end
                local f = load(reader)
                return f()
