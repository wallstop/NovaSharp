-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/EndToEnd/SimpleTUnitTests.cs:1566
-- @test: SimpleTUnitTests.MissingArgsDefaultToNil
function test(a)
					return a;
				end

				test();
