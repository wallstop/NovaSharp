-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/SimpleTUnitTests.cs:874
-- @test: SimpleTUnitTests.OperatorPrecedence5
return 3 * -1 + 5 * 3
