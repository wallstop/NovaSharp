-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/BasicModuleTUnitTests.cs:935
-- @test: BasicModuleTUnitTests.PrintIgnoresGlobalTostringForPlainTablesInLua54Plus
-- @compat-notes: Test targets Lua 5.4+
function tostring(v)
                    return 'CUSTOM:' .. type(v)
                end
                t = {}  -- plain table, no metatable
                print(t)
