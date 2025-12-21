-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/TableModuleTUnitTests.cs:337
-- @test: TableModuleTUnitTests.RemoveErrorsOnNonIntegerPositionLua53Plus
-- @compat-notes: Test targets Lua 5.3+
table.remove({1,2,3}, 1.5)
