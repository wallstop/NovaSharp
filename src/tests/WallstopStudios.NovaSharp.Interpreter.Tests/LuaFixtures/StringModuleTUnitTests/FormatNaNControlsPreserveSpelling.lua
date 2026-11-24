-- @lua-versions: all
-- @novasharp-only: false
-- @expects-error: false
local function check_nan(formatted)
    local normalized = formatted:lower():gsub("^%-", ""):gsub("%(ind%)$", "")
    assert(normalized == "nan", "expected a NaN spelling, got [" .. formatted .. "]")
end

local nan = 0 / 0
local decimal = string.format("%g", nan)
check_nan(decimal)

local hex = "unsupported"
if _VERSION ~= "Lua 5.1" then
    hex = string.format("%A", nan)
    check_nan(hex)
end

return decimal, hex
