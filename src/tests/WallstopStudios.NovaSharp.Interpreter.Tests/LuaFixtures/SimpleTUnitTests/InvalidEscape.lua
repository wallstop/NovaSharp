-- @lua-versions: 5.3+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\EndToEnd\SimpleTUnitTests.cs:283
-- @test: SimpleTUnitTests.InvalidEscape
-- @compat-notes: Test targets Lua 5.3+
x = 'ciao\k{41}';
				return x;
