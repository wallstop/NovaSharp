-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathNumericEdgeCasesTUnitTests.cs:462
-- @test: MathNumericEdgeCasesTUnitTests.NaNNotLessThanItself
-- @compat-notes: Test targets Lua 5.1
local nan = 0/0; return nan < nan
