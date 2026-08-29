-- @lua-versions: 5.1+
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/DataTypes/CallbackFunctionTUnitTests.cs:489
-- @test: CallbackFunctionTUnitTests.ArgumentViewCountsForwardedMultiReturnsWithoutVoidSentinel
local function values(m)
    if m == 0 then return end
    return m, values(m - 1)
end
local function nothing() end
return countArgs(values(5)), countArgs(nothing()), countArgs(7, 8, 9)
