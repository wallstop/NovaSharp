-- @lua-versions: 5.3+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\Utf8ModuleTUnitTests.cs:128
-- @test: Utf8ModuleTUnitTests.Utf8CharBuildsStringsFromCodePoints
-- Test targets Lua 5.3+; Lua 5.3+: utf8 library
return utf8.char(0x41, 0x1F600, 0x20AC)
