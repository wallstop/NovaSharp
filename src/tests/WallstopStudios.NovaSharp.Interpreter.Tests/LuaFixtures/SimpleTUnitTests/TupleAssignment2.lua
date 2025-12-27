-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/SimpleTUnitTests.cs:1192
-- @test: SimpleTUnitTests.TupleAssignment2
function boh()
					return 1, 2;
				end

				x,y,z = boh(), boh()

				return x,y,z;
