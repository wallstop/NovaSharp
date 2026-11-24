-- @lua-versions: all
-- @novasharp-only: false
-- @expects-error: false
local conversions = { "e", "E", "f", "g", "G" }
local hex_supported = _VERSION ~= "Lua 5.1"
local allows_repeated_flags = hex_supported and _VERSION ~= "Lua 5.2" and _VERSION ~= "Lua 5.3"
if hex_supported then
    conversions[#conversions + 1] = "a"
    conversions[#conversions + 1] = "A"
else
    assert(not pcall(string.format, "%a", math.huge))
    assert(not pcall(string.format, "%A", math.huge))
end

local function check(format, value, expected)
    local actual = string.format(format, value)
    assert(actual == expected, format .. ": expected [" .. expected .. "], got [" .. actual .. "]")
end

for _, conversion in ipairs(conversions) do
    local inf = conversion == string.upper(conversion) and "INF" or "inf"
    local cases = {
        { "", inf, "-" .. inf },
        { "2", inf, "-" .. inf },
        { " ", " " .. inf, "-" .. inf },
        { " +", "+" .. inf, "-" .. inf },
        { "9", "      " .. inf, "     -" .. inf },
        { "09", "      " .. inf, "     -" .. inf },
        { "-09", inf .. "      ", "-" .. inf .. "     " },
        { "0+9", "     +" .. inf, "     -" .. inf },
        { "#.2", inf, "-" .. inf },
        { ".", inf, "-" .. inf },
        { "99", string.rep(" ", 96) .. inf, string.rep(" ", 95) .. "-" .. inf },
    }

    for _, case in ipairs(cases) do
        local format = "%" .. case[1] .. conversion
        check(format, math.huge, case[2])
        check(format, -math.huge, case[3])
    end

    check("%-----9" .. conversion, math.huge, inf .. "      ")
    local repeated = "%------9" .. conversion
    if allows_repeated_flags then
        check(repeated, math.huge, inf .. "      ")
        local longest = "%" .. string.rep("-", 19) .. "9" .. conversion
        check(longest, -math.huge, "-" .. inf .. "     ")
        local too_long = "%" .. string.rep("-", 20) .. "9" .. conversion
        local ok, message = pcall(string.format, too_long, math.huge)
        assert(not ok and message:find("invalid format (too long)", 1, true), message)
    else
        local ok, message = pcall(string.format, repeated, math.huge)
        assert(not ok and message:find("invalid format (repeated flags)", 1, true), message)
    end
end

for _, invalid in ipairs({ "%ıf", "%ı.2f", "%1ıf", "%.ıf", "%.1ıf" }) do
    assert(not pcall(string.format, invalid, math.huge), "accepted invalid format " .. invalid)
end
local mixed_flags = "%000000+9.2f"
if allows_repeated_flags then
    check(mixed_flags, math.huge, "     +inf")
end
local finite_ok, finite = pcall(string.format, mixed_flags, 1.5)
assert((finite_ok and finite == "+00001.50")
    or (not finite_ok and finite:find("invalid format (repeated flags)", 1, true)), finite)
local nan_ok, nan = pcall(string.format, mixed_flags, 0 / 0)
assert((nan_ok and nan:lower():find("nan", 1, true))
    or (not nan_ok and nan:find("invalid format (repeated flags)", 1, true)), nan)
assert(not pcall(string.format, "%++++++9s", "text"))
assert(string.format("%g, %G, %+g end", math.huge, -math.huge, 1.5) == "inf, -INF, +1.5 end")
check("%+09.2f", 1.5, "+00001.50")
if hex_supported then
    check("%012.1a", 1.5, "0x00001.8p+0")
end

return true
