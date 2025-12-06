-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Units/BitwiseOperatorTUnitTests.cs:19
-- @test: BitwiseOperatorTUnitTests.BitwiseOperatorsRequireLua53Compatibility
-- @compat-notes: Test targets Lua 5.2+; Lua 5.3+: bitwise AND
return 1 & 1
