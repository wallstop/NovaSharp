-- @lua-versions: all
-- @novasharp-only: false
-- @expects-error: false

local nul = string.char(0)

local function yields(expected, action)
  local ok, value = pcall(action)
  return ok and value == expected
end

local function probe(label, text, number, base16)
  print(label,
    yields(-number, function() return -text end),
    yields(number - 2, function() return text - 2 end),
    yields(2 - number, function() return 2 - text end),
    tonumber(text) == number,
    tonumber(text, 10) == number,
    tonumber(text, 16) == base16,
    yields(17, function() return tonumber("10", text) end),
    yields(number, function()
      for i = text, 32 do return i end
    end),
    yields(1, function()
      for i = 1, text do return i end
    end),
    yields(1, function()
      for i = 1, 32, text do return i end
    end))
end

probe("valid", "17", 17, 23)
probe("trailing", "17" .. nul, 17, 23)
probe("embedded", "17" .. nul .. "8", 17, 23)
probe("leading", nul .. "17", 17, 23)

print("hex prefix", tonumber("0x11" .. nul .. "bad") == 17)
print("float prefix", tonumber("1.75" .. nul .. "bad") == 1.75)

if math.tointeger then
  local complement = assert(load("return function(value) return ~value end"))()
  local band = assert(load("return function(value) return value & 3 end"))()
  for _, text in ipairs({ "17" .. nul, "17" .. nul .. "8", nul .. "17" }) do
    local unaryOk = pcall(complement, text)
    local binaryOk = pcall(band, text)
    print("integer", math.tointeger(text) == nil, unaryOk, binaryOk)
  end
end
