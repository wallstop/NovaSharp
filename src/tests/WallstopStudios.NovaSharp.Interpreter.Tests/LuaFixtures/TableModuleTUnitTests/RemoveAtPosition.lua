-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\TableModuleTUnitTests.cs:777
-- @test: TableModuleTUnitTests.RemoveAtPosition
-- @compat-notes: NovaSharp: unresolved C# interpolation placeholder
local t = {{'a', 'b', 'c', 'd'}}
                local removed = table.remove(t, {position})
                return removed, table.concat(t, '-')
