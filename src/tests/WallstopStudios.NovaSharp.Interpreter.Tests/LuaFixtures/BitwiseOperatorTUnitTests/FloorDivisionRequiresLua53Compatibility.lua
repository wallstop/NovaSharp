-- @lua-versions: 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/BitwiseOperatorTUnitTests.cs:43
-- @test: BitwiseOperatorTUnitTests.FloorDivisionRequiresLua53Compatibility
-- @compat-notes: Test targets Lua 5.1; Lua 5.3+: floor division
return 5 // 2
