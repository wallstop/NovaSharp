-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathNumericEdgeCasesTUnitTests.cs:347
-- @test: MathNumericEdgeCasesTUnitTests.NaNNotGreaterThanItself
-- @compat-notes: Test targets Lua 5.4+; Lua 5.3+: bitwise operators
local nan = 0/0; return nan > nan
