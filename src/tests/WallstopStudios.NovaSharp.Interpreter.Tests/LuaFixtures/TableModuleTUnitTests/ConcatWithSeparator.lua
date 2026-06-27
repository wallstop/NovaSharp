-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\TableModuleTUnitTests.cs:627
-- @test: TableModuleTUnitTests.ConcatWithSeparator
-- NovaSharp: unresolved C# interpolation placeholder
local t = {{'a', 'b', 'c', 'd'}}
                return table.concat(t, '{separator}')
