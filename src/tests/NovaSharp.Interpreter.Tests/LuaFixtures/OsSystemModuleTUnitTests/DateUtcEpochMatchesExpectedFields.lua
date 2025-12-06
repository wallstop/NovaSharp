-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/OsSystemModuleTUnitTests.cs:244
-- @test: OsSystemModuleTUnitTests.DateUtcEpochMatchesExpectedFields
-- @compat-notes: Lua 5.3+: bitwise operators
local t = os.date('!*t', 0)
                return t.year, t.month, t.day, t.hour, t.min, t.sec, t.wday, t.yday, t.isdst
