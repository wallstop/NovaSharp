-- @lua-versions: all
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/TableTUnitTests.cs:193
-- @test: TableTUnitTests.TableLengthFollowsVersionedConstructorBorders
local function expect(label, actual, lua51to53, lua54, lua55)
    local expected = lua51to53
    if _VERSION == 'Lua 5.4' then
        expected = lua54
    elseif _VERSION == 'Lua 5.5' then
        expected = lua55
    end

    assert(actual == expected, label .. ': expected ' .. expected .. ', got ' .. actual)
end

expect('{nil, 1}', #({ nil, 1 }), 2, 2, 0)
expect('{1, nil, 3}', #({ 1, nil, 3 }), 3, 3, 1)
expect('{1, nil}', #({ 1, nil }), 1, 1, 1)
expect('{nil, 1, nil}', #({ nil, 1, nil }), 0, 2, 0)
expect('{1, 2, nil, 4}', #({ 1, 2, nil, 4 }), 4, 4, 2)
expect('{1, nil, 3, nil, 5}', #({ 1, nil, 3, nil, 5 }), 5, 5, 1)

local assigned = {}
assigned[2] = 1
expect('assigned sparse table', #assigned, 0, 0, 0)

local stringMutated = { nil, 1 }
stringMutated.x = 1
expect('string-mutated constructor table', #stringMutated, 0, 0, 0)

local valueMutated = { nil, 1 }
valueMutated[false] = 1
expect('value-key-mutated constructor table', #valueMutated, 0, 0, 0)

local overwritten = { nil, 1 }
overwritten[2] = 1
expect('same constructor slot overwritten', #overwritten, 2, 2, 0)

local cachedThenOverwritten = { nil, 1 }
local _ = #cachedThenOverwritten
cachedThenOverwritten[2] = 1
expect('cached same constructor slot overwritten', #cachedThenOverwritten, 2, 2, 0)

local nilNoOp = { nil, 1 }
nilNoOp[1] = nil
expect('constructor nil slot written nil', #nilNoOp, 2, 2, 0)

local filledLeadingNil = { nil, 1 }
filledLeadingNil[1] = 1
expect('constructor leading nil filled', #filledLeadingNil, 2, 2, 2)

local overwrittenTail = { 1, nil, 3 }
overwrittenTail[3] = 4
expect('constructor tail overwritten', #overwrittenTail, 3, 3, 1)

local cachedThenOverwrittenTail = { 1, nil, 3 }
_ = #cachedThenOverwrittenTail
cachedThenOverwrittenTail[3] = 4
expect('cached constructor tail overwritten', #cachedThenOverwrittenTail, 3, 3, 1)

local filledInteriorNil = { 1, nil, 3, nil, 5 }
filledInteriorNil[4] = 4
expect('constructor interior nil filled', #filledInteriorNil, 5, 5, 1)

local removedLeadingValue = { 1, nil, 3, nil, 5 }
removedLeadingValue[1] = nil
expect('constructor leading value removed', #removedLeadingValue, 5, 5, 0)

local cachedThenStringMutated = { nil, 1 }
_ = #cachedThenStringMutated
cachedThenStringMutated.x = 1
expect('cached string-mutated constructor table', #cachedThenStringMutated, 0, 0, 0)

local absentStringNil = { nil, 1 }
absentStringNil.x = nil
expect('absent string nil write', #absentStringNil, 0, 2, 0)

local absentValueNil = { nil, 1 }
absentValueNil[false] = nil
expect('absent value nil write', #absentValueNil, 0, 2, 0)

local cachedThenAbsentStringNil = { nil, 1 }
_ = #cachedThenAbsentStringNil
cachedThenAbsentStringNil.x = nil
expect('cached absent string nil write', #cachedThenAbsentStringNil, 0, 2, 0)

expect('mixed constructor string field', #({ nil, 1, x = 1 }), 2, 2, 0)
expect('mixed constructor value field', #({ nil, 1, [false] = 1 }), 2, 2, 0)
expect('mixed constructor nil string field', #({ nil, 1, x = nil }), 2, 2, 0)
expect('mixed constructor string field before array fields', #({ x = 1, nil, 1 }), 2, 2, 0)
expect('numeric constructor field extends border', #({ nil, 1, [3] = 1 }), 3, 3, 0)
expect('nil numeric constructor field does not extend border', #({ nil, 1, [3] = nil }), 2, 2, 0)
expect('sparse numeric constructor field does not extend border', #({ nil, 1, [4] = 1 }), 2, 2, 0)
expect('contiguous numeric constructor fields extend border', #({ nil, 1, [3] = 1, [4] = 1 }), 4, 4, 0)
expect('out-of-order contiguous numeric constructor fields extend border', #({ nil, 1, [4] = 1, [3] = 1 }), 4, 4, 0)
expect('numeric-only constructor first index', #({ [1] = 1 }), 1, 1, 1)
expect('numeric-only constructor sparse index', #({ [2] = 1 }), 0, 0, 0)
expect('numeric-only constructor contiguous indices', #({ [1] = 1, [2] = 1 }), 2, 2, 2)
