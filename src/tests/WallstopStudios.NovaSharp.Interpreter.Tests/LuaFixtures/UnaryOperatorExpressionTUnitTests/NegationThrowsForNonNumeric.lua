-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Units\Tree\Expressions\UnaryOperatorExpressionTUnitTests.cs:43
-- @test: UnaryOperatorExpressionTUnitTests.NegationThrowsForNonNumeric
return -"hello"
