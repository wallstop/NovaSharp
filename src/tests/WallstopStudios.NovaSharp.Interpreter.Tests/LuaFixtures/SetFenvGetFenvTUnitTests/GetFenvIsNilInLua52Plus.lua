-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\SetFenvGetFenvTUnitTests.cs:58
-- @test: SetFenvGetFenvTUnitTests.GetFenvIsNilInLua52Plus
-- Test targets Lua 5.1
return getfenv
