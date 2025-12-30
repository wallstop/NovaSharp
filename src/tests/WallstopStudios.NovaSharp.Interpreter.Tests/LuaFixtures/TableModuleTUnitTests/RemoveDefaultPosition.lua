-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/TableModuleTUnitTests.cs:809
-- @test: TableModuleTUnitTests.RemoveDefaultPosition
local t = {'a', 'b', 'c'}
                local removed = table.remove(t)
                return removed, table.concat(t, '-'), #t
