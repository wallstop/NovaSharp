-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/OsSystemModuleTUnitTests.cs:266
-- @test: OsSystemModuleTUnitTests.DateUtcEpochMatchesExpectedFields
-- @compat-notes: Test class 'OsSystemModuleTUnitTests' uses NovaSharp-specific OsSystemModule functionality
local t = os.date('!*t', 0)
                return t.year, t.month, t.day, t.hour, t.min, t.sec, t.wday, t.yday, t.isdst
