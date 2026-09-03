-- @lua-versions: all
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/NumericLiteralTUnitTests.cs
-- @test: NumericLiteralTUnitTests.NumericLiteralsMaterializePerCompatibilityVersion
-- Integer-syntax literals round to IEEE 754 doubles in Lua 5.1/5.2 and keep
-- integer subtypes (hex wrapping modulo 2^64) in Lua 5.3+.
local x = 9007199254740993
print(x, x == 9007199254740992, x - 9007199254740992)
print(9223372036854775807, 18446744073709551616, 99999999999999999999)
print(0x89abcdef, 0xffffffffffffffff, 0x10000000000000000)
print(0xdeadbeefdeadbeefdeadbeef, 123456789012345678)
local t = {}
t[9007199254740992] = "a"
print(t[9007199254740992.0], t[9007199254740993])
t[0x10] = "hex"
print(t[16], t[16.0])
local acc = {}
for i = 4503599627370490, 4503599627370499 do acc[#acc + 1] = i end
print(#acc, acc[1], acc[#acc])
print(-9007199254740993, -0xffffffffffffffff, -(0xffffffffffffffff))
print(1 == 1.0, 0x10 == 16.0, 2^53 == 9007199254740992)
