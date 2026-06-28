-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Units\Compatibility\Bit32CompatibilityWarningTUnitTests.cs:59
-- @test: Bit32CompatibilityWarningTUnitTests.RequireBit32InLua52DoesNotEmitWarning
-- Test class 'Bit32CompatibilityWarningTUnitTests' uses NovaSharp-specific Bit32CompatibilityWarning functionality
return require('bit32') ~= nil
