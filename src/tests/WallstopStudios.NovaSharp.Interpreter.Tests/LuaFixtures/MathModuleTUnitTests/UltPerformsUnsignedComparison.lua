-- @lua-versions: 5.3+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs:203
-- @test: MathModuleTUnitTests.UltPerformsUnsignedComparison
return math.ult(0, -1), math.ult(-1, 0)
