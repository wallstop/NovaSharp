-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\TableModuleTUnitTests.cs:744
-- @test: TableModuleTUnitTests.InsertAtPosition
-- @compat-notes: NovaSharp: unresolved C# interpolation placeholder
local t = {{'a', 'b', 'c'}}
                table.insert(t, {position}, '{value}')
                return table.concat(t, '-')
