-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/TableModuleTUnitTests.cs:105
-- @test: TableModuleTUnitTests.SortThrowsWhenComparatorIsInvalidType
local values = { 1, 2 }
                local ok, message = pcall(function() table.sort(values, {}) end)
                assert(not ok, 'invalid comparator')
                assert(message:find('bad argument #2', 1, true), message)
                assert(message:find('function expected, got table', 1, true), message)
                assert(values[1] == 1 and values[2] == 2, 'sort did not mutate values')
