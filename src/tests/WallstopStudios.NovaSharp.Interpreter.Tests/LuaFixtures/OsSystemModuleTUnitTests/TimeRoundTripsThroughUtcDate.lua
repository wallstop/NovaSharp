-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/OsSystemModuleTUnitTests.cs:339
-- @test: OsSystemModuleTUnitTests.TimeRoundTripsThroughUtcDate
-- @compat-notes: Lua 5.3+: bitwise operators
local stamp = os.time({
                    year = 2000,
                    month = 1,
                    day = 1,
                    hour = 0,
                    min = 0,
                    sec = 0,
                    isdst = 0,
                })
                local t = os.date('!*t', stamp)
                return stamp, t.year, t.month, t.day, t.hour, t.min, t.sec
