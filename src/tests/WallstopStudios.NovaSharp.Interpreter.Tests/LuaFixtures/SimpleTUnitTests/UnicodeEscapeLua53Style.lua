-- @lua-versions: 5.3+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\EndToEnd\SimpleTUnitTests.cs:266
-- @test: SimpleTUnitTests.UnicodeEscapeLua53Style
-- Test targets Lua 5.3+
x = 'ciao\u{41}';
				return x;
