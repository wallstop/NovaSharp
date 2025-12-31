-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\TableModuleTUnitTests.cs:888
-- @test: TableModuleTUnitTests.SortByStringLength
local t = {'a', 'bbb', 'cc', 'dddd'}
                table.sort(t, function(a, b) return #a < #b end)
                return table.concat(t, '-')
