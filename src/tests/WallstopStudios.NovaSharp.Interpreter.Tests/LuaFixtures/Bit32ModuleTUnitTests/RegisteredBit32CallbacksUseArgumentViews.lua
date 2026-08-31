-- @lua-versions: 5.2
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/Bit32ModuleTUnitTests.cs:102
-- @test: Bit32ModuleTUnitTests.RegisteredBit32CallbacksUseArgumentViews
-- Compatibility notes: Lua 5.2 only: bit32 library (5.2 only, removed in 5.3+)
local expected_exports = {
    arshift = true,
    band = true,
    bnot = true,
    bor = true,
    btest = true,
    bxor = true,
    extract = true,
    lrotate = true,
    lshift = true,
    replace = true,
    rrotate = true,
    rshift = true,
}
local export_count = 0
for name, value in pairs(bit32) do
    assert(expected_exports[name], "unexpected bit32 export: " .. tostring(name))
    assert(type(value) == "function")
    export_count = export_count + 1
end
assert(export_count == 12)
assert(bit32.band() == 0xffffffff)
assert(bit32.bor() == 0)
assert(bit32.bxor() == 0)
assert(bit32.btest() == true)
assert(bit32.band(5.7, 3) == 2)
assert(bit32.band(-1.5, 0xffffffff) == 0xfffffffe)
assert(bit32.band(2^51 + 1, 0xffffffff) == 0)
assert(bit32.band(-(2^51 + 1), 0xffffffff) == 0xfffffffe)
assert(bit32.band(2^53 + 2, 0xffffffff) == 1)
assert(bit32.band(-(2^53 + 2), 0xffffffff) == 4)
assert(bit32.band(2^63 - 1024, 0xffffffff) == 0)
assert(bit32.band(-(2^63 - 1024), 0xffffffff) == 0xffffffff)
assert(bit32.band(1e20, 0xffffffff) == 2025163840)
assert(bit32.band(-1e20, 0xffffffff) == 2025163840)
assert(bit32.band("1e20", 0xffffffff) == 2025163840)
assert(bit32.band(9.007199254740995e15, 0xffffffff) == 2)
assert(bit32.btest(9.007199254740995e15, 4) == false)
assert(bit32.bnot(9.007199254740995e15) == 0xfffffffd)
assert(bit32.lshift(9.007199254740995e15, 0) == 2)
assert(bit32.extract(9.007199254740995e15, 1, 1) == 1)
assert(bit32.replace(0, 9.007199254740995e15, 0, 32) == 2)
assert(bit32.band(1.7976931348623157e308, 0xffffffff) == 0xffffffff)
assert(bit32.band(-1.7976931348623157e308, 0xffffffff) == 0xffffffff)
assert(bit32.band(5e-324, 0xffffffff) == 0)
assert(bit32.band(-5e-324, 0xffffffff) == 0)
assert(bit32.band(math.huge, 0xffffffff) == 0)
assert(bit32.band(-math.huge, 0xffffffff) == 0)
assert(bit32.band(0 / 0, 0xffffffff) == 0)
assert(bit32.lshift(1, 32) == 0)
assert(bit32.rshift(0xffffffff, 32) == 0)
assert(bit32.arshift(0x80000000, 32) == 0xffffffff)
assert(bit32.arshift(1, 32) == 0)
assert(bit32.rshift(8, -1.7) == 16)
assert(bit32.lrotate(1, -1.7) == 0x80000000)
assert(bit32.extract(1, -0.5) == 1)
assert(bit32.replace(0, 1, -0.5) == 1)
assert(bit32.extract(0xf0, 4, 4) == 15)
assert(bit32.extract(0xf0, 4, nil) == 1)
assert(bit32.replace(0, 1, 3, nil) == 8)
assert(bit32.extract(0xf0, 4, "4") == 15)
assert(bit32.replace(0, 15, 4, "4") == 240)
local extreme_value = 0x89abcdef
local below_int64_limit = 2^63 - 1024
assert(bit32.lshift(extreme_value, below_int64_limit) == 0)
assert(bit32.rshift(extreme_value, below_int64_limit) == 0)
assert(bit32.arshift(extreme_value, below_int64_limit) == 0)
assert(bit32.lrotate(extreme_value, below_int64_limit) == extreme_value)
assert(bit32.rrotate(extreme_value, below_int64_limit) == extreme_value)
-- Reference Lua builds can differ for the most extreme displacement values
-- because their C integer/non-finite narrowing is architecture-sensitive. The
-- exact narrowing matrix is covered by the NovaSharp C# regression test instead.
local above_double_precision = 9007199254740993
assert(bit32.band(above_double_precision, 0xffffffff) == 0)
assert(bit32.extract(1, above_double_precision) == 1)
assert(bit32.replace(0, 1, above_double_precision) == 1)
local ok_large_width, large_width_error = pcall(function()
    bit32.extract(1, 0, above_double_precision)
end)
assert(not ok_large_width and string.find(large_width_error, "width must be positive", 1, true))
local ok_field, field_error = pcall(function() bit32.extract(0, -1, 34) end)
assert(not ok_field and string.find(field_error, "field cannot be negative", 1, true))
local ok_width, width_error = pcall(function() bit32.replace(0, 1, 32, 0) end)
assert(not ok_width and string.find(width_error, "width must be positive", 1, true))
local ok_nan_width, nan_width_error = pcall(function() bit32.extract(1, 0, 0 / 0) end)
assert(not ok_nan_width and string.find(nan_width_error, "width must be positive", 1, true))
local ok_band, band_error = pcall(function() bit32.band(false) end)
assert(not ok_band and string.find(band_error, "to 'band'", 1, true))
local ok_extract_type, extract_type_error = pcall(function() bit32.extract(0, 0, false) end)
assert(not ok_extract_type)
assert(string.find(extract_type_error,
    "bad argument #3 to 'extract' (number expected, got boolean)", 1, true))
local ok_replace_type, replace_type_error = pcall(function() bit32.replace(0, 0, 0, false) end)
assert(not ok_replace_type)
assert(string.find(replace_type_error,
    "bad argument #4 to 'replace' (number expected, got boolean)", 1, true))
print("bit32 callback view parity")
