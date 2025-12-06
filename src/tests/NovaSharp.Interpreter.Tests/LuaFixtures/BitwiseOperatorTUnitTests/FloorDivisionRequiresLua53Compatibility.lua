-- @lua-versions: 5.2, 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Units/BitwiseOperatorTUnitTests.cs:35
-- @test: BitwiseOperatorTUnitTests.FloorDivisionRequiresLua53Compatibility
-- @compat-notes: Test targets Lua 5.2+
return 5 // 2
