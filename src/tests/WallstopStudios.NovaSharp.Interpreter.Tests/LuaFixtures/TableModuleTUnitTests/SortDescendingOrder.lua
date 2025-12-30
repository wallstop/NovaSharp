-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/TableModuleTUnitTests.cs:842
-- @test: TableModuleTUnitTests.SortDescendingOrder
local t = {3, 1, 4, 1, 5, 9, 2, 6}
                table.sort(t, function(a, b) return a > b end)
                return table.concat(t, '-')
