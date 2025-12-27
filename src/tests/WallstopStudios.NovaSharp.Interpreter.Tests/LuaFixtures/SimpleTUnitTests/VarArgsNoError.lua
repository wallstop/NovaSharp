-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/SimpleTUnitTests.cs:1353
-- @test: SimpleTUnitTests.VarArgsNoError
-- @compat-notes: Test targets Lua 5.2+
function x(...)

					end

					function y(a, ...)

					end

					return 1;
