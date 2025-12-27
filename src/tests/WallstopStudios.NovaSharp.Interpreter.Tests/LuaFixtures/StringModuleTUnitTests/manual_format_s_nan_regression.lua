-- Tests string.format("%s", nan) behavior.
-- NaN formatting varies by platform/implementation but should contain "nan".
-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false

local nan_str = string.format("%s", 0 / 0)
local nan_upper = nan_str:upper()

-- NaN representation can be "nan", "NaN", "-nan", "-NaN", etc.
if not nan_upper:find("NAN") then
    error(string.format("FAIL: string.format('%%s', 0/0) = '%s', should contain 'nan' (case-insensitive)", nan_str))
end

print("PASS: NaN formatting test passed - result was: " .. nan_str)