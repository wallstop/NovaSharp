-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/SimpleTUnitTests.cs:926
-- @test: SimpleTUnitTests.OperatorParenthesis
return (5+3)*7-2*5+(2^3)^2
