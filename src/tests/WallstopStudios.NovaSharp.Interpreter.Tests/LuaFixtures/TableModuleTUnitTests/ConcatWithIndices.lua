-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/TableModuleTUnitTests.cs:661
-- @test: TableModuleTUnitTests.ConcatWithIndices
-- @compat-notes: NovaSharp: unresolved C# interpolation placeholder
local t = {{'a', 'b', 'c', 'd'}}
                return table.concat(t, '-', {startIndex}, {endIndex})
