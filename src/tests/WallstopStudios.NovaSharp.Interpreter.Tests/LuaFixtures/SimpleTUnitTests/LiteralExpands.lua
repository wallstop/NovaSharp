-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/SimpleTUnitTests.cs:1580
-- @test: SimpleTUnitTests.LiteralExpands
x = 'a\65\66\67z';
				return x;
