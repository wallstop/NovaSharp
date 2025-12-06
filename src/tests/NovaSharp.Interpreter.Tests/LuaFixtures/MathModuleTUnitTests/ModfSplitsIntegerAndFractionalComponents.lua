-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs:53
-- @test: MathModuleTUnitTests.ModfSplitsIntegerAndFractionalComponents
return math.modf(-3.25)
