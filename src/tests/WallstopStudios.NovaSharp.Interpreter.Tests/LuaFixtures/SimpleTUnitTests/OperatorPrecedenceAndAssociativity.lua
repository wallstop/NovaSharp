-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/SimpleTUnitTests.cs:913
-- @test: SimpleTUnitTests.OperatorPrecedenceAndAssociativity
return 5+3*7-2*5+2^3^2
