-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathNumericEdgeCasesTUnitTests.cs:394
-- @test: MathNumericEdgeCasesTUnitTests.NaNNotLessThanItself
local nan = 0/0; return nan < nan
