-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/OsTimeModuleTUnitTests.cs:98
-- @test: OsTimeModuleTUnitTests.TimeReturnsNegativeForDatesBeforeEpoch
-- @compat-notes: Platform-dependent. Standard Lua's behavior depends on the underlying C mktime().
-- On many Unix systems, pre-epoch dates throw "time result cannot be represented in this installation".
-- NovaSharp returns negative timestamps for pre-epoch dates (valid on 64-bit systems).

-- Test that NovaSharp returns negative timestamp for pre-1970 dates (NovaSharp-specific behavior)
local result = os.time({
    year = 1969,
    month = 12,
    day = 31,
    hour = 23,
    min = 59,
    sec = 59
})

if type(result) == "number" and result < 0 then
    print("PASS")
else
    error("Expected negative number for pre-epoch date, got: " .. tostring(result))
end
