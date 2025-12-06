-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs:68
-- @test: MathModuleTUnitTests.MaxAggregatesAcrossArguments
return math.max(-1, 5, 12, 3)
