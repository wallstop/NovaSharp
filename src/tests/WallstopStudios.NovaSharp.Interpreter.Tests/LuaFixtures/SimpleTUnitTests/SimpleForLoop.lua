-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/SimpleTUnitTests.cs:1036
-- @test: SimpleTUnitTests.SimpleForLoop
x = 0
					for i = 1, 3 do
						x = x + i;
					end

					return x;
