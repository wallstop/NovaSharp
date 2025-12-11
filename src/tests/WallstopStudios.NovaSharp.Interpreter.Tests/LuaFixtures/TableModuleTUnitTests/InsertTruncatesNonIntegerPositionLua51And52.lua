-- @lua-versions: 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\TableModuleTUnitTests.cs:261
-- @test: TableModuleTUnitTests.InsertTruncatesNonIntegerPositionLua51And52
-- @compat-notes: Test targets Lua 5.1; Lua 5.3+: bitwise operators
local t = {1, 2, 3}
                table.insert(t, 1.9, 'x')
                return t[1]
