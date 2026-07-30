-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/SimpleTUnitTests.cs:1123
-- @test: SimpleTUnitTests.FunctionWithStringArg2
x = 0;

				fact = function(y)
					x = y
				end

				fact 'ciao';

				return x;
