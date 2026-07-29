-- @lua-versions: 5.2+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\TableModuleTUnitTests.cs:522
-- @test: TableModuleTUnitTests.GlobalUnpackIsNilInLua52Plus
-- Test targets Lua 5.2+
return unpack
