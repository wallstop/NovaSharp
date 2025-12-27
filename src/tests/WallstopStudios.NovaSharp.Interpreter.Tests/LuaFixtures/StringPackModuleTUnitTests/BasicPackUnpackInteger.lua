-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringPackModuleTUnitTests.cs
-- @test: StringPackModuleTUnitTests.BasicPackUnpackInteger

-- Test: Basic pack/unpack of integers (string.pack is Lua 5.3+ only)

local packed = string.pack("i4", 42)
local unpacked, nextpos = string.unpack("i4", packed)
print(unpacked)

-- Test round-trip
local packed2 = string.pack("j", 42)
local unpacked2 = string.unpack("j", packed2)
print(unpacked2)
