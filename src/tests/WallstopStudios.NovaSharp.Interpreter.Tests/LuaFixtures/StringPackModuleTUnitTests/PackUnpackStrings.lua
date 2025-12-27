-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringPackModuleTUnitTests.cs
-- @test: StringPackModuleTUnitTests.PackUnpackStrings

-- Test: Pack/unpack of string types (z, s, c formats)

-- Zero-terminated string
local packed_z = string.pack("z", "hello")
local unpacked_z = string.unpack("z", packed_z)
print(unpacked_z)

-- Length-prefixed string
local packed_s = string.pack("s4", "world")
local unpacked_s = string.unpack("s4", packed_s)
print(unpacked_s)

-- Fixed-size string (padded with zeros)
local packed_c = string.pack("c8", "fixed")
local unpacked_c = string.unpack("c8", packed_c)
-- Trim null padding for display
print(unpacked_c:gsub("%z+$", ""))
