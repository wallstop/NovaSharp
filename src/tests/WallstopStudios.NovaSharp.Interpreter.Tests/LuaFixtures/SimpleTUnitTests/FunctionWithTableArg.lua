-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/SimpleTUnitTests.cs:1171
-- @test: SimpleTUnitTests.FunctionWithTableArg
x = 0;

				function fact(y)
					x = y
				end

				fact { 1,2,3 };

				return x;
