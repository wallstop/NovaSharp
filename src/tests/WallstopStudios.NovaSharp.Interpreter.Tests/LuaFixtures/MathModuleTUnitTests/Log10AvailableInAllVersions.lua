-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs:1266
-- @test: MathModuleTUnitTests.Log10AvailableInAllVersions
return math.log10(100)
