-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/SimpleTUnitTests.cs:1353
-- @test: SimpleTUnitTests.VarArgsNoError
function x(...)

					end

					function y(a, ...)

					end

					return 1;
