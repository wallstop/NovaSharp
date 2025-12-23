-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringPackModuleTUnitTests.cs
-- @test: StringPackModuleTUnitTests.PackUnpackMultipleValues

-- Test: Packing and unpacking multiple values at once

local packed = string.pack("i4 i4 i4 z", 1, 2, 3, "hello")
local a, b, c, s = string.unpack("i4 i4 i4 z", packed)
print(a)
print(b)
print(c)
print(s)
