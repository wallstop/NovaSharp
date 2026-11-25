-- @lua-versions: 5.3+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs:2615
-- @test: MathModuleTUnitTests.ExtremaPreserveOriginalOperands
local i, f = 9007199254740993, 9007199254740992.0
local max, min = math.max(f, i), math.min(i, f)
assert(math.type(max) == 'integer' and string.format('%d', max) == '9007199254740993')
assert(math.type(min) == 'float' and min == f)
assert(math.min(math.mininteger + 1, math.mininteger) == math.mininteger)
assert(math.max(math.maxinteger - 1, math.maxinteger) == math.maxinteger)
assert(math.max(math.maxinteger, 9223372036854775808.0) == 9223372036854775808.0)
assert(math.min(math.mininteger + 1, -9223372036854775808.0) == -9223372036854775808.0)
assert(math.type(math.min(1.0, 1)) == 'float' and math.type(math.max(1, 1.0)) == 'integer')
assert(1 / math.min(-0.0, 0) == -math.huge and 1 / math.max(0.0, -0.0) == math.huge)

local nan = 0 / 0
for _, f in ipairs({math.min, math.max}) do
    assert(f(1, nan, 1) == 1 and f(nan, 1) ~= f(nan, 1))
    assert(math.type(f(math.maxinteger)) == 'integer')
    assert(f(nil) == nil and select('#', f(nil)) == 1)
    assert(f(false) == false and rawequal(f(f), f))
    local ok, err = pcall(f)
    assert(not ok and err:find('(value expected)', 1, true))
end

assert(math.min('10', '2') == '10' and math.max('10', '2') == '2')
local prefix, extended = 'a', 'a' .. string.char(0, 255)
assert(math.min(extended, prefix) == prefix and math.max(prefix, extended) == extended)
local ok, err = pcall(math.min, 1, '2')
assert(not ok and err:find('attempt to compare string with number', 1, true))
ok, err = pcall(math.max, 1, '2')
assert(not ok and err:find('attempt to compare number with string', 1, true))
return true
