-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\TableModuleTUnitTests.cs:240
-- @test: TableModuleTUnitTests.InsertErrorsOnNonIntegerPositionLua53Plus
-- @compat-notes: Test targets Lua 5.1
table.insert({1,2,3}, 1.5, 'x')
