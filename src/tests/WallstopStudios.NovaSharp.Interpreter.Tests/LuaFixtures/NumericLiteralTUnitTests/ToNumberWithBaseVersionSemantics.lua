-- @lua-versions: all
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/NumericLiteralTUnitTests.cs
-- @test: NumericLiteralTUnitTests.ToNumberWithBaseMatchesReference
-- tonumber(v, base) follows strtoul semantics in Lua 5.1, double accumulation in
-- 5.2, and modulo-2^64 integers in 5.3+; 5.1 treats base 10 as the standard
-- conversion. The number-argument cases are guarded so Lua 5.3+ (which reject
-- them) stay comparable.
print(tonumber('7f', 16), tonumber('ffffffffffffffff', 16))
print(tonumber('10000000000000000', 16), tonumber('fffffffffffffffff', 16))
print(tonumber('7g', 16), tonumber('17', 6))
print(tonumber('  7f  ', 16), tonumber('+7f', 16))
print(tonumber('0x10'))
if _VERSION == 'Lua 5.1' or _VERSION == 'Lua 5.2' then
    print(tonumber('-ff', 16))
    print(tonumber('0x11', 10), tonumber('3.14', 10), tonumber('0x10', 16))
    print(tonumber(111, 2), tonumber(4294967295, 16))
else
    print(tonumber('-ff', 16))
    print(tonumber('0x11', 10), tonumber('3.14', 10), tonumber('0x10', 16))
end
