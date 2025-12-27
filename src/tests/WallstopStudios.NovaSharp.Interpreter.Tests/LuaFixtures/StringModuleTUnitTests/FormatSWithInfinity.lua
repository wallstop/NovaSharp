-- Tests string.format("%s", infinity) behavior.
-- Infinity formatting varies by platform/implementation.
-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false

-- Test positive infinity - result should contain "inf" (case-insensitive)
local pos_inf = string.format("%s", 1 / 0)
local pos_inf_upper = pos_inf:upper()
if not pos_inf_upper:find("INF") then
  error(string.format("FAIL: string.format('%%s', 1/0) = '%s', should contain 'inf'", pos_inf))
end

-- Test negative infinity - result should start with "-" and contain "inf"
local neg_inf = string.format("%s", -1 / 0)
local neg_inf_upper = neg_inf:upper()
if not neg_inf:match("^%-") then
  error(string.format("FAIL: string.format('%%s', -1/0) = '%s', should start with '-'", neg_inf))
end
if not neg_inf_upper:find("INF") then
  error(string.format("FAIL: string.format('%%s', -1/0) = '%s', should contain 'inf'", neg_inf))
end

print("PASS: Infinity formatting tests passed")