-- @lua-versions: 5.1, 5.2
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/TableModuleTUnitTests.cs:313
-- @test: TableModuleTUnitTests.InsertTruncatesNonIntegerPositionLua51And52
-- @compat-notes: Test targets Lua 5.1
local t = {1, 2, 3}
                table.insert(t, 1.9, 'x')
                return t[1]
