-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/BasicModuleTUnitTests.cs:905
-- @test: BasicModuleTUnitTests.PrintUsesTostringMetamethodDirectlyInLua54Plus
-- @compat-notes: Test targets Lua 5.4+
function tostring(v)
                    return 'CUSTOM:' .. type(v)
                end
                t = setmetatable({}, { __tostring = function() return 'META' end })
                print(t)
