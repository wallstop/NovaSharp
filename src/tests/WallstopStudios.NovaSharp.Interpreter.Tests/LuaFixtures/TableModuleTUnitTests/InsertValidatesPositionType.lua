-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/TableModuleTUnitTests.cs:213
-- @test: TableModuleTUnitTests.InsertValidatesPositionType
-- @compat-notes: Test targets Lua 5.1
local values = {}
                    table.insert(values, 'two', 99)
