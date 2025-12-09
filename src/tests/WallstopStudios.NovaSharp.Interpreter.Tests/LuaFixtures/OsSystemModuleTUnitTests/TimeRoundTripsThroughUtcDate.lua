-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\OsSystemModuleTUnitTests.cs:364
-- @test: OsSystemModuleTUnitTests.TimeRoundTripsThroughUtcDate
-- @compat-notes: Test class 'OsSystemModuleTUnitTests' uses NovaSharp-specific OsSystemModule functionality
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
