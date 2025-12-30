-- @lua-versions: 5.2
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Compatibility/Bit32CompatibilityWarningTUnitTests.cs:59
-- @test: Bit32CompatibilityWarningTUnitTests.RequireBit32InLua52DoesNotEmitWarning
-- @compat-notes: bit32 module only available by default in Lua 5.2
return require('bit32') ~= nil