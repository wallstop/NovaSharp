-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/OsTimeModuleTUnitTests.cs:123
-- @test: OsTimeModuleTUnitTests.TimeReturnsNegativeForDatesBeforeEpoch
-- @compat-notes: Test targets Lua 5.1
return os.time({
                    year = 1969,
                    month = 12,
                    day = 31,
                    hour = 23,
                    min = 59,
                    sec = 59
                })
