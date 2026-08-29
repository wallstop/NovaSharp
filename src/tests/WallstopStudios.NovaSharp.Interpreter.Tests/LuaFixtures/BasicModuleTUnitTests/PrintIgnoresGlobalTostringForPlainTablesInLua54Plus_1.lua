-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/BasicModuleTUnitTests.cs:1427
-- @test: BasicModuleTUnitTests.PrintIgnoresGlobalTostringForPlainTablesInLua54Plus
-- Compatibility notes: Test targets Lua 5.4+
function tostring(v)
                    return 'CUSTOM:' .. type(v)
                end
                no_field = setmetatable({}, {})
                nil_field = setmetatable({}, { __tostring = nil })
                print(no_field)
                print(nil_field)
