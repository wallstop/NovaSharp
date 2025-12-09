-- @lua-versions: 5.3+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs:179
-- @test: MathModuleTUnitTests.ToIntegerThrowsWhenArgumentMissing
return math.tointeger()
