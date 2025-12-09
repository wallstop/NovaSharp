-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/TableModuleTUnitTests.cs:169
-- @test: TableModuleTUnitTests.InsertValidatesPositionType
-- @compat-notes: Lua 5.3+: bitwise operators
local values = {}
                    table.insert(values, 'two', 99)
