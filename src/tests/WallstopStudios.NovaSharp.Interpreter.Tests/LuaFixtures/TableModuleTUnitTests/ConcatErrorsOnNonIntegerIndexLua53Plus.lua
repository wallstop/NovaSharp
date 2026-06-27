-- @lua-versions: 5.3+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\TableModuleTUnitTests.cs:331
-- @test: TableModuleTUnitTests.ConcatErrorsOnNonIntegerIndexLua53Plus
-- Test targets Lua 5.3+
table.concat({'a','b','c'}, '', 1.5)
