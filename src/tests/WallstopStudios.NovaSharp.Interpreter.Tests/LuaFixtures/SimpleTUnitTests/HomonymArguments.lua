-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/SimpleTUnitTests.cs:1598
-- @test: SimpleTUnitTests.HomonymArguments
-- @compat-notes: Test targets Lua 5.2+
function test(_,value,_) return _; end

				return test(1, 2, 3);
