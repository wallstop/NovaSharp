-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/TableModuleTUnitTests.cs:895
-- @test: TableModuleTUnitTests.SortStringElements
local t = {'banana', 'apple', 'cherry', 'date'}
                table.sort(t)
                return table.concat(t, '-')
