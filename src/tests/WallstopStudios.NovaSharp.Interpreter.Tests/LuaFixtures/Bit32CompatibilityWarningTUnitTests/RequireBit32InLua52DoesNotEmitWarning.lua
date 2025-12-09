-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Units\Compatibility\Bit32CompatibilityWarningTUnitTests.cs:56
-- @test: Bit32CompatibilityWarningTUnitTests.RequireBit32InLua52DoesNotEmitWarning
-- @compat-notes: Test targets Lua 5.2+
return require('bit32') ~= nil
