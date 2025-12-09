-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/TableModuleTUnitTests.cs:329
-- @test: TableModuleTUnitTests.UnpackErrorsOnNonIntegerIndexLua53Plus
-- @compat-notes: Test targets Lua 5.3+; Lua 5.2+: table.unpack (5.2+)
table.unpack({1,2,3}, 1.5)
