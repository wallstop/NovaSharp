-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/BasicModuleTUnitTests.cs:875
-- @test: BasicModuleTUnitTests.PrintCallsGlobalTostringInLua51To53
-- @compat-notes: Test targets Lua 5.1
function tostring(v)
                    return 'CUSTOM:' .. type(v)
                end
                t = setmetatable({}, { __tostring = function() return 'META' end })
                print(t)
