-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/SimpleTUnitTests.cs:1598
-- @test: SimpleTUnitTests.HomonymArguments
function test(_,value,_) return _; end

				return test(1, 2, 3);
