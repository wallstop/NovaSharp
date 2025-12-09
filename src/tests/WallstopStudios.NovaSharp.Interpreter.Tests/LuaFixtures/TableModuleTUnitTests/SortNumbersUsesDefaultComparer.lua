-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/TableModuleTUnitTests.cs:50
-- @test: TableModuleTUnitTests.SortNumbersUsesDefaultComparer
-- @compat-notes: Lua 5.3+: bitwise operators
local values = { 4, 1, 3 }
                table.sort(values)
                return values[1], values[2], values[3]
