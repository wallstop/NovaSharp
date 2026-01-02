-- @lua-versions: 5.2+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\LoadModuleVersionParityTUnitTests.cs:180
-- @test: LoadModuleVersionParityTUnitTests.LoadAcceptsReaderFunctionInLua52Plus
-- @compat-notes: Test targets Lua 5.2+
local done = false
                local function reader()
                    if done then return nil end
                    done = true
                    return 'return 456'
                end
                local f = load(reader)
                return f()
