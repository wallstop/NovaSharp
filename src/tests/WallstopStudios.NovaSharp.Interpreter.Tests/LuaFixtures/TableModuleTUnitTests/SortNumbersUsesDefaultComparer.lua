-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/TableModuleTUnitTests.cs:64
-- @test: TableModuleTUnitTests.SortNumbersUsesDefaultComparer
-- @compat-notes: Test targets Lua 5.1
local values = { 4, 1, 3 }
                table.sort(values)
                return values[1], values[2], values[3]
