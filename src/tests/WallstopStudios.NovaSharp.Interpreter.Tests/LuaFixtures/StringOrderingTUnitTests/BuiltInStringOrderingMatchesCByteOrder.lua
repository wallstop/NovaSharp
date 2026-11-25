-- @lua-versions: all
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/StringOrderingTUnitTests.cs:12
-- @test: StringOrderingTUnitTests.BuiltInStringOrderingMatchesCByteOrder

os.setlocale('C', 'collate')

-- The literals are U+E000 and U+10000, whose UTF-8 and UTF-16 orders differ.
assert('' < '𐀀' and '' <= '𐀀', 'constant comparisons must use Lua byte order')
local bmp, astral = '', '𐀀'
assert(bmp < astral and bmp <= astral and astral > bmp and astral >= bmp)
assert(not (astral < bmp) and not (astral <= bmp))

local rawAstral = string.char(240, 144, 128, 128)
local rawAstralNext = string.char(240, 144, 128, 129)
local rawHigh = '\255'
assert(astral <= rawAstral and rawAstral <= astral)
assert(not (astral < rawAstral) and not (rawAstral < astral))
assert(astral < rawAstralNext and astral < rawHigh and not (rawHigh <= astral))
assert('\000' < '\000a' and '\000a' < '\000b')
assert('a' < 'a\000b' and 'a\000' < 'a\001')
assert(('a\000' .. bmp) < ('a\000' .. astral))
assert(('a\000' .. astral) <= ('a\000' .. rawAstral))
assert(not ('same' < 'same') and 'same' <= 'same')

local typeMetatable = debug.getmetatable('')
debug.setmetatable('', { __lt = function() error('string ordering called __lt') end })
local values = {astral, rawHigh, 'Z\000w', bmp, 'Z'}
table.sort(values)
assert(values[1] == 'Z' and values[2] == 'Z\000w')
assert(values[3] == bmp and values[4] == astral and values[5] == rawHigh)
assert(bmp < astral)
if math.type then
    assert(math.min(astral, bmp) == bmp and math.max(bmp, astral) == astral)
    assert(math.max(rawHigh, astral) == rawHigh)
end

local order = {[bmp] = 2, [astral] = 1}
local custom = {bmp, astral}
table.sort(custom, function(left, right) return order[left] < order[right] end)
assert(custom[1] == astral and custom[2] == bmp)
debug.setmetatable('', typeMetatable)
print('C locale string order verified')
