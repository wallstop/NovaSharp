-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/TableModuleTUnitTests.cs:293
-- @test: TableModuleTUnitTests.InsertErrorsOnNonIntegerPositionLua53Plus
-- @compat-notes: Test targets Lua 5.1
table.insert({1,2,3}, 1.5, 'x')
