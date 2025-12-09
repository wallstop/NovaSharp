-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathNumericEdgeCasesTUnitTests.cs:238
-- @test: MathNumericEdgeCasesTUnitTests.IntegerModuloByZeroReturnsNaNInLua51And52
-- @compat-notes: Test targets Lua 5.1
return 5 % 0
