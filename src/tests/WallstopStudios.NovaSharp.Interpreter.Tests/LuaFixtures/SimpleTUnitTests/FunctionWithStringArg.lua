-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/SimpleTUnitTests.cs:1147
-- @test: SimpleTUnitTests.FunctionWithStringArg
x = 0;

				function fact(y)
					x = y
				end

				fact 'ciao';

				return x;
