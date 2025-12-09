-- @lua-versions: 5.2
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Compatibility/Bit32CompatibilityWarningTUnitTests.cs:56
-- @test: Bit32CompatibilityWarningTUnitTests.RequireBit32InLua52DoesNotEmitWarning
-- @compat-notes: NovaSharp-specific warning system test. bit32 is built-in in Lua 5.2.
-- Note: NovaSharp's compatibility warning system is an extension not present in standard Lua.

local bit32_module = require('bit32')
if bit32_module ~= nil then
    print("PASS")
else
    error("Expected bit32 to be available in Lua 5.2")
end
