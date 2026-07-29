-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/SimpleTUnitTests.cs:1279
-- @test: SimpleTUnitTests.ExpressionReducesTuples
function x()
						return 1,2
					end

					do return (x()); end
					do return x(); end
