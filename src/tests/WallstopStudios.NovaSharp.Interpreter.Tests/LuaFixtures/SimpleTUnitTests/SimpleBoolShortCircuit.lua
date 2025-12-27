-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/SimpleTUnitTests.cs:445
-- @test: SimpleTUnitTests.SimpleBoolShortCircuit
x = true or crash();
				y = false and crash();
