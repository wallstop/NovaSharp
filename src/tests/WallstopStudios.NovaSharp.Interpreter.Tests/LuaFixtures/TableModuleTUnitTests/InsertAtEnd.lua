-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/TableModuleTUnitTests.cs:707
-- @test: TableModuleTUnitTests.InsertAtEnd
local t = {1, 2, 3}
                table.insert(t, 4)
                return t[1], t[2], t[3], t[4], #t
