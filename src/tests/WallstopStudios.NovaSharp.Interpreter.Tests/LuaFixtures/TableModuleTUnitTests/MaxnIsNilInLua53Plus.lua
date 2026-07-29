-- @lua-versions: 5.3+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\TableModuleTUnitTests.cs:563
-- @test: TableModuleTUnitTests.MaxnIsNilInLua53Plus
-- Test targets Lua 5.3+
return table.maxn
