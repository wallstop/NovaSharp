-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\TableModuleTUnitTests.cs:310
-- @test: TableModuleTUnitTests.ConcatErrorsOnNonIntegerIndexLua53Plus
-- @compat-notes: Test targets Lua 5.3+
table.concat({'a','b','c'}, '', 1.5)
