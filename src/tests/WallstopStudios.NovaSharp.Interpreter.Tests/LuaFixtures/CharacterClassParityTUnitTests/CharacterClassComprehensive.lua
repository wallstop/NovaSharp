-- Test: Comprehensive character class verification across ASCII range
-- Expected: Output showing which characters match each class
-- Versions: 5.1, 5.2, 5.3, 5.4
-- Reference: Lua 5.4 §6.4.1 - Patterns
-- Purpose: Verify NovaSharp character classes match reference Lua

-- Character classes being tested:
-- %a - letters (alphabetic)
-- %c - control characters
-- %d - digits
-- %g - printable characters except space (Lua 5.2+)
-- %l - lowercase letters
-- %p - punctuation characters
-- %s - space characters
-- %u - uppercase letters
-- %w - alphanumeric characters
-- %x - hexadecimal digits

local function test_class(class_char, description)
    local result = {}
    for i = 0, 127 do
        local c = string.char(i)
        local pattern = "%" .. class_char
        if string.match(c, pattern) then
            table.insert(result, i)
        end
    end
    return result
end

local function format_results(name, codes)
    local parts = {}
    for _, code in ipairs(codes) do
        table.insert(parts, tostring(code))
    end
    return name .. ": " .. table.concat(parts, ",")
end

-- Test all character classes
local classes = {
    {"a", "alpha"},
    {"c", "control"},
    {"d", "digit"},
    {"l", "lower"},
    {"p", "punct"},
    {"s", "space"},
    {"u", "upper"},
    {"w", "alnum"},
    {"x", "xdigit"},
}

-- %g was added in Lua 5.2
if _VERSION >= "Lua 5.2" then
    table.insert(classes, {"g", "graph"})
end

for _, class_info in ipairs(classes) do
    local class_char, desc = class_info[1], class_info[2]
    local codes = test_class(class_char)
    print(format_results("%" .. class_char .. " (" .. desc .. ")", codes))
end
