-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs:334
-- @test: MathModuleTUnitTests.FloorPreservesIntegerInput
return math.floor(42)
