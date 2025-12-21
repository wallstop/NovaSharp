-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Tree/Expressions/UnaryOperatorExpressionTUnitTests.cs:54
-- @test: UnaryOperatorExpressionTUnitTests.NegationThrowsForNonNumeric
-- @compat-notes: Test targets Lua 5.1
return -"hello"
