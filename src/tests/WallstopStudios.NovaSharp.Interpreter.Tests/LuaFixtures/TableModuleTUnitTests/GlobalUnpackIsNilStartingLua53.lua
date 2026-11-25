-- @lua-versions: 5.3+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/TableModuleTUnitTests.cs:720
-- @test: TableModuleTUnitTests.GlobalUnpackIsNilStartingLua53
-- Test targets Lua 5.3+
return unpack
