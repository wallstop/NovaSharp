-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/SimpleTUnitTests.cs:1563
-- @test: SimpleTUnitTests.TupleToOperator
function x()
					return 3, 'xx';
				end

				return x() == 3;
