-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathNumericEdgeCasesTUnitTests.cs:381
-- @test: MathNumericEdgeCasesTUnitTests.NaNNotEqualToItself
local nan = 0/0; return nan == nan
