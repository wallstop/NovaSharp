-- @lua-versions: 5.3+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\TableModuleTUnitTests.cs:308
-- @test: TableModuleTUnitTests.RemoveErrorsOnNonIntegerPositionLua53Plus
-- Test targets Lua 5.3+
table.remove({1,2,3}, 1.5)
