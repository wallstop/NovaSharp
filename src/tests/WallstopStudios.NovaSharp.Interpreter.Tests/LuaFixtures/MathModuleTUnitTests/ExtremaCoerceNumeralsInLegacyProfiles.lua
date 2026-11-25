-- @lua-versions: 5.1-5.2
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs:2710
-- @test: MathModuleTUnitTests.ExtremaCoerceNumeralsInLegacyProfiles
local nan = 0 / 0
for _, f in ipairs({math.min, math.max}) do
    assert(type(f('10')) == 'number' and f('10') == 10)
    assert(f('0x10') == 16 and 1 / f('-0') == -math.huge)
    assert(f(1, nan) == 1 and f(nan, 1) ~= f(nan, 1))
    local ok, err = pcall(f)
    assert(not ok and err:find('number expected, got no value', 1, true))
    for _, invalid in ipairs({'not a numeral', '1,000', {}, false}) do
        ok, err = pcall(f, 1, invalid)
        assert(not ok and err:find('bad argument #2', 1, true))
    end
    ok, err = pcall(f, nil)
    assert(not ok and err:find('number expected, got nil', 1, true))
    ok, err = pcall(f, '17' .. string.char(0) .. '2')
    if _VERSION == 'Lua 5.1' then
        assert(ok and err == 17)
    else
        assert(not ok and err:find('number expected, got string', 1, true))
    end
end
assert(math.min('10', '2') == 2 and math.max('10', '2') == 10)
return true
