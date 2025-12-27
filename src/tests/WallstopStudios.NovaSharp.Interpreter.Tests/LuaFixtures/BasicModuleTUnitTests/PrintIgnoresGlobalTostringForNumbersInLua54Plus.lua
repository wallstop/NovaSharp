-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/BasicModuleTUnitTests.cs:1030
-- @test: BasicModuleTUnitTests.PrintIgnoresGlobalTostringForNumbersInLua54Plus
-- @compat-notes: Test targets Lua 5.1
function tostring(v)
                    if type(v) == 'number' then
                        return 'NUM:' .. v
                    end
                    return v
                end
                print(42)
