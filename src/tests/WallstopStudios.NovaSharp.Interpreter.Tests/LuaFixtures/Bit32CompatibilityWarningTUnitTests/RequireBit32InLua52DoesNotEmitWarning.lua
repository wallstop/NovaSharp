-- @lua-versions: 5.2
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Units\Compatibility\Bit32CompatibilityWarningTUnitTests.cs:56
-- @test: Bit32CompatibilityWarningTUnitTests.RequireBit32InLua52DoesNotEmitWarning
-- @compat-notes: NovaSharp extension - bit32 is only available natively in Lua 5.2; NovaSharp provides it as a compatibility shim
return require('bit32') ~= nil
