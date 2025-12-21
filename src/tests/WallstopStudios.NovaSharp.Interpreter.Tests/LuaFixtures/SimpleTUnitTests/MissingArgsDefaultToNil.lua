-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/SimpleTUnitTests.cs:1720
-- @test: SimpleTUnitTests.MissingArgsDefaultToNil
function test(a)
					return a;
				end

				test();
