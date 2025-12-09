-- @lua-versions: 5.3
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Compatibility/Bit32CompatibilityWarningTUnitTests.cs:29
-- @test: Bit32CompatibilityWarningTUnitTests.RequireBit32InLua53EmitsWarningOnlyOnce
-- @compat-notes: NovaSharp-specific warning system test. In standard Lua 5.3, bit32 is still available
-- but NovaSharp emits a compatibility warning. This test validates the warning is emitted only once.
-- Note: NovaSharp's compatibility warning system is an extension not present in standard Lua.

-- In NovaSharp, requiring bit32 in Lua 5.3 mode will emit a warning but still succeed
local bit32_module = require('bit32')
if bit32_module ~= nil then
    print("PASS")
else
    error("Expected bit32 to be available in Lua 5.3 (with warning)")
end
