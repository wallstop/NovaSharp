-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringPackModuleTUnitTests.cs
-- @test: StringPackModuleTUnitTests.PackUnpackFloat

-- Test: Pack/unpack of floating point numbers (f, d formats)

-- Test float (32-bit)
local packed_f = string.pack("f", 3.14)
local unpacked_f = string.unpack("f", packed_f)
print(string.format("%.2f", unpacked_f))

-- Test double (64-bit)
local packed_d = string.pack("d", 2.71828)
local unpacked_d = string.unpack("d", packed_d)
print(string.format("%.5f", unpacked_d))
