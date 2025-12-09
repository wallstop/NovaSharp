-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/TableModuleTUnitTests.cs:348
-- @test: TableModuleTUnitTests.MoveErrorsOnNonIntegerArgLua53Plus
-- @compat-notes: Test targets Lua 5.3+; Lua 5.3+: table.move
table.move({1,2,3}, 1.5, 2, 1)
