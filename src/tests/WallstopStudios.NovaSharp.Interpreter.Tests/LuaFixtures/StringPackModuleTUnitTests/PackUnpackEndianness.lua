-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringPackModuleTUnitTests.cs
-- @test: StringPackModuleTUnitTests.PackUnpackEndianness

-- Test: Endianness options for pack/unpack (<, > prefixes)

-- Little endian: 0x0100 = 256, low byte first
local packed_le = string.pack("<I2", 1)
local unpacked_be, _ = string.unpack(">I2", packed_le)
print(unpacked_be)

-- Big endian: 0x0001 = 1, high byte first
local packed_be = string.pack(">I2", 1)
local unpacked_le, _ = string.unpack("<I2", packed_be)
print(unpacked_le)
