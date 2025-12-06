-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs:31
-- @test: MathModuleTUnitTests.LogSupportsCustomBase
return math.log(8, 2)
