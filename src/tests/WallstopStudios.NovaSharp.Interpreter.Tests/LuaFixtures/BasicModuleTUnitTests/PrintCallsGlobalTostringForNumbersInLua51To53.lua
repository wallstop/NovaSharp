-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/BasicModuleTUnitTests.cs:998
-- @test: BasicModuleTUnitTests.PrintCallsGlobalTostringForNumbersInLua51To53
-- @compat-notes: Test targets Lua 5.1
function tostring(v)
                    if type(v) == 'number' then
                        return 'NUM:' .. v
                    end
                    return v
                end
                print(42)
