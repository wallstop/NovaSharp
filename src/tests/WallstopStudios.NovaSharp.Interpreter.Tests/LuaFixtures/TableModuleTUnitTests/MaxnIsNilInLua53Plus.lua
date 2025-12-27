-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/TableModuleTUnitTests.cs:563
-- @test: TableModuleTUnitTests.MaxnIsNilInLua53Plus
-- @compat-notes: Test targets Lua 5.3+
return table.maxn
