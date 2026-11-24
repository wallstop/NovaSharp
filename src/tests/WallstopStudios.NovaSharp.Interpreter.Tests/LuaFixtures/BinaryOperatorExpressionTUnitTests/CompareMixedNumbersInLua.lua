-- @lua-versions: all
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Tree/Expressions/BinaryOperatorExpressionTUnitTests.cs:CompareMixedNumbersInLua
-- @test: BinaryOperatorExpressionTUnitTests.CompareMixedNumbersInLua
local hasIntegers = math.type ~= nil

local function compare(label, a, b, less, greater, equal)
    assert((a < b) == less, label .. ' <')
    assert((b < a) == greater, label .. ' reverse <')
    assert((a <= b) == (less or equal), label .. ' <=')
    assert((b <= a) == (greater or equal), label .. ' reverse <=')
    assert((a > b) == greater, label .. ' >')
    assert((a >= b) == (greater or equal), label .. ' >=')
    assert((a == b) == equal, label .. ' ==')
    assert((b == a) == equal, label .. ' reverse ==')
    assert((a ~= b) == not equal, label .. ' ~=')
    assert(rawequal(a, b) == equal, label .. ' rawequal')
    assert(rawequal(b, a) == equal, label .. ' reverse rawequal')
end

local wide, rounded = 9007199254740993, 9007199254740992.0
compare('2^53 precision', wide, rounded, false, hasIntegers, not hasIntegers)
compare('negative 2^53 precision', -wide, -rounded, hasIntegers, false, not hasIntegers)
compare('exact large integral float', 9007199254740992, rounded, false, false, true)
assert((9007199254740992.0 < 9007199254740993) == hasIntegers, 'literal <')
assert((9007199254740993 <= 9007199254740992.0) == not hasIntegers, 'literal <=')
assert((9007199254740993 == 9007199254740992.0) == not hasIntegers, 'literal ==')

local max = math.maxinteger or 9223372036854775807
local min = math.mininteger or -9223372036854775808
compare('upper bound', max, 9223372036854775808.0, hasIntegers, false, not hasIntegers)
compare('inside upper bound', max, 9223372036854774784.0, false, true, false)
compare('lower bound', min, -9223372036854775808.0, false, false, true)
compare('inside lower bound', min + 1, -9223372036854775808.0, false, hasIntegers, not hasIntegers)
compare('below lower bound', min, -9223372036854777856.0, false, true, false)
compare('negative fraction', 0, -0.5, false, true, false)
compare('positive fraction', 0, 0.5, true, false, false)
compare('positive infinity', max, math.huge, true, false, false)
compare('negative infinity', min, -math.huge, false, true, false)
compare('unordered NaN', 0, 0 / 0, false, false, false)

local zero = -1 / math.huge
compare('signed zero', 0, zero, false, false, true)
assert(1 / zero == -math.huge, 'negative zero keeps its sign')

local keys = {}
keys[wide] = 'wide'
keys[rounded] = 'rounded'
assert(keys[wide] == (hasIntegers and 'wide' or 'rounded'), 'adjacent key')
assert(keys[9007199254740992] == 'rounded', 'integral float key alias')
keys[max] = 'max'
keys[9223372036854775808.0] = 'upper'
assert(keys[max] == (hasIntegers and 'max' or 'upper'), 'upper key')
keys[min] = 'min'
keys[zero] = 'zero'
assert(keys[-9223372036854775808.0] == 'min', 'lower key alias')
assert(keys[0] == 'zero', 'signed zero key alias')

-- Before Lua 5.3, arithmetic must round even if the engine happens to store
-- an integral library result or length in an integer slot.
local bound, one = math.floor(rounded), #'a'
local sum = bound + one
local product = math.floor(3002399751580331.0) * (one + one + one)
compare('arithmetic sum', sum, rounded, false, hasIntegers, not hasIntegers)
compare('arithmetic difference', -bound - one, -rounded, hasIntegers, false, not hasIntegers)
compare('arithmetic product', product, rounded, false, hasIntegers, not hasIntegers)
compare('subsequent subtraction', sum - bound, hasIntegers and 1 or 0, false, false, true)
local remainder = math.floor(9007199254740994.0) % #'abc'
compare('profile-specific modulo', remainder, hasIntegers and 1 or 2, false, false, true)

local fromLength = one
for _ = 1, 53 do
    fromLength = fromLength + fromLength
end
compare('arithmetic from length', fromLength + one, rounded, false, hasIntegers, not hasIntegers)

local negatedMin = -math.floor(-9223372036854775808.0)
compare('unary minus at integer limit', negatedMin, min, false, not hasIntegers, hasIntegers)

local summedKeys = {}
summedKeys[sum] = 'sum'
summedKeys[rounded] = 'rounded'
assert(summedKeys[sum] == (hasIntegers and 'sum' or 'rounded'), 'arithmetic key')
if not hasIntegers then
    assert(next(summedKeys, rounded) == nil, 'arithmetic key cursor')
end

assert((-(-9223372036854775807) < 9223372036854775808.0) == hasIntegers, 'negated literal')
local iterations = 0
for _ = bound, rounded, math.floor(1.0) do
    iterations = iterations + 1
    if iterations == 3 then break end
end
assert(iterations == (hasIntegers and 1 or 3), 'profile-specific numeric for')

iterations = 0
for _ = wide, rounded, 1.0 do
    iterations = iterations + 1
    if iterations == 3 then break end
end
assert(iterations == 3, 'float numeric for')
