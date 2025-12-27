-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathVersionCompatibilityTUnitTests.cs:419
-- @test: MathVersionCompatibilityTUnitTests.MathHugeAvailableInAllVersions
return math.huge
