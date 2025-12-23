-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringPackModuleTUnitTests.cs
-- @test: StringPackModuleTUnitTests.PackUnpackByteTypes

-- Test: Signed/unsigned byte types (b, B formats)

-- Signed byte (-1 = 0xFF)
local packed_b = string.pack("b", -1)
local unpacked_b = string.unpack("b", packed_b)
print(unpacked_b)

-- Unsigned byte (255 = 0xFF)
local packed_B = string.pack("B", 255)
local unpacked_B = string.unpack("B", packed_B)
print(unpacked_B)
