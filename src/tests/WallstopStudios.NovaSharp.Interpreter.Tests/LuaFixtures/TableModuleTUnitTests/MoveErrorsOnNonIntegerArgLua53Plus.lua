-- @lua-versions: 5.3+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\TableModuleTUnitTests.cs:375
-- @test: TableModuleTUnitTests.MoveErrorsOnNonIntegerArgLua53Plus
-- Test targets Lua 5.3+; Lua 5.3+: table.move
table.move({1,2,3}, 1.5, 2, 1)
