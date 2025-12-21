-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/SimpleTUnitTests.cs:1299
-- @test: SimpleTUnitTests.ExpressionReducesTuples2
function x()
						return 3,4
					end

					return 1,x(),x()
