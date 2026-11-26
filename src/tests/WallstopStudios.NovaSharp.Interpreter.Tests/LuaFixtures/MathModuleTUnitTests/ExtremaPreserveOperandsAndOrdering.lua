-- @lua-versions: all
-- @novasharp-only: false
-- @expects-error: false
local modern = _VERSION ~= 'Lua 5.1' and _VERSION ~= 'Lua 5.2'

if modern then
    -- Relational comparison keeps the selected operand's exact subtype and identity.
    local wide, rounded = 9007199254740993, 9007199254740992.0
    local maximum = math.max(rounded, wide)
    assert(math.type(maximum) == 'integer', 'max keeps the integer operand')
    assert(string.format('%d', maximum) == '9007199254740993', 'max keeps full integer precision')
    local minimum = math.min(wide, rounded)
    assert(math.type(minimum) == 'float', 'min keeps the float operand')
    assert(minimum == 9007199254740992.0, 'min keeps the rounded float')
    local low = math.max(math.mininteger, -9223372036854775808.0)
    assert(math.type(low) == 'integer' and low == math.mininteger, 'max keeps the lower bound')
    local high = math.min(math.maxinteger, 9223372036854775808.0)
    assert(math.type(high) == 'integer' and high == math.maxinteger, 'min keeps the upper bound')

    -- Ties and lone operands return the first operand unchanged.
    assert(math.type(math.max(1, 1.0)) == 'integer', 'max keeps the first tie')
    assert(math.type(math.min(1.0, 1)) == 'float', 'min keeps the first tie')
    assert(math.max(math.maxinteger) == math.maxinteger, 'max returns a lone integer')
    assert(math.type(math.max(math.maxinteger)) == 'integer', 'max returns a lone integer subtype')
    assert(math.min(nil) == nil, 'min returns a lone nil')
    local lone = setmetatable({}, { __name = 'Lone' })
    assert(math.max(lone) == lone, 'max returns a lone table identity')

    -- Strings compare as strings instead of coercing to numbers.
    assert(math.min('10', '2') == '10', 'min compares strings lexicographically')
    assert(math.max('10', '2') == '2', 'max compares strings lexicographically')

    -- Signed zero and NaN follow relational comparison results.
    assert(1 / math.max(0.0, -0.0) == math.huge, 'max keeps positive zero on a tie')
    assert(1 / math.min(0.0, -0.0) == math.huge, 'min keeps positive zero on a tie')
    local nan = 0 / 0
    assert(math.max(nan, 1) ~= math.max(nan, 1), 'max keeps a leading NaN')
    assert(math.min(1, nan) == 1, 'min ignores a trailing NaN')

    -- Comparison hooks preserve operand order and table identity.
    local trace = ''
    local mt = {
        __lt = function(left, right)
            trace = trace .. left.tag .. right.tag
            return left.tag < right.tag
        end,
    }
    local first = setmetatable({ tag = '1' }, mt)
    local second = setmetatable({ tag = '2' }, mt)
    assert(math.max(first, second) == second and trace == '12', 'max uses __lt operands')
    trace = ''
    assert(math.min(first, second) == first and trace == '21', 'min uses __lt operands')

    -- Failed comparisons use raw metatable __name labels.
    local ok, err = pcall(math.max, 1, setmetatable({}, { __name = 'Actor' }))
    assert(not ok, 'max rejects an incomparable operand')
    assert(err:match('attempt to compare.*') == 'attempt to compare number with Actor', tostring(err))
    ok, err = pcall(math.min, setmetatable({}, { __name = 'Actor' }), 1)
    assert(not ok, 'min rejects an incomparable operand')
    assert(err:match('attempt to compare.*') == 'attempt to compare number with Actor', tostring(err))
    local actorLeft = setmetatable({}, { __name = 'Actor' })
    local actorRight = setmetatable({}, { __name = 'Actor', __metatable = 'locked' })
    ok, err = pcall(math.max, actorLeft, actorRight)
    assert(not ok, 'max rejects equal custom names')
    assert(err:match('attempt to compare.*') == 'attempt to compare two Actor values', tostring(err))
    local unnamedLeft = setmetatable({}, { __name = 123 })
    local unnamedRight = setmetatable({}, { __name = 123 })
    ok, err = pcall(math.min, unnamedLeft, unnamedRight)
    assert(not ok, 'min rejects non-string names')
    assert(err:match('attempt to compare.*') == 'attempt to compare two table values', tostring(err))
    ok, err = pcall(math.max, 1, {})
    assert(not ok, 'max rejects an unnamed table')
    assert(err:match('attempt to compare.*') == 'attempt to compare number with table', tostring(err))

    -- A missing argument reports the value diagnostic, not a numeric one.
    ok, err = pcall(math.max)
    assert(not ok and err:find('value expected', 1, true), tostring(err))
else
    -- Lua 5.1 and 5.2 validate numbers and coerce numeric strings.
    local minimum = math.min('10', '2')
    assert(type(minimum) == 'number' and minimum == 2, 'min coerces numeric strings')
    local maximum = math.max('10', '2')
    assert(type(maximum) == 'number' and maximum == 10, 'max coerces numeric strings')
    local ok, err = pcall(math.min, 1, setmetatable({}, {}))
    assert(not ok and err:find('number expected', 1, true), tostring(err))
    ok, err = pcall(math.max)
    assert(not ok and err:find('number expected', 1, true), tostring(err))
end
