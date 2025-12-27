-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathNumericEdgeCasesTUnitTests.cs:407
-- @test: MathNumericEdgeCasesTUnitTests.NaNNotGreaterThanItself
-- @compat-notes: Test targets Lua 5.3+
local nan = 0/0; return nan > nan
