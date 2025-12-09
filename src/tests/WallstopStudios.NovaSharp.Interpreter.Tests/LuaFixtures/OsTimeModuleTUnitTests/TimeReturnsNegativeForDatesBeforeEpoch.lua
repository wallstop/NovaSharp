-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\OsTimeModuleTUnitTests.cs:98
-- @test: OsTimeModuleTUnitTests.TimeReturnsNegativeForDatesBeforeEpoch
-- @compat-notes: Lua 5.3+: bitwise operators
return os.time({
                    year = 1969,
                    month = 12,
                    day = 31,
                    hour = 23,
                    min = 59,
                    sec = 59
                })
