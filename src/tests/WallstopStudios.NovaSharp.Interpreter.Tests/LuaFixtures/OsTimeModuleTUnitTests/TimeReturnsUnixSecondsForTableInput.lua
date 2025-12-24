-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/OsTimeModuleTUnitTests.cs:26
-- @test: OsTimeModuleTUnitTests.TimeReturnsUnixSecondsForTableInput
-- @compat-notes: Test targets Lua 5.1
return os.time({
                    year = 1970,
                    month = 1,
                    day = 2,
                    hour = 0,
                    min = 0,
                    sec = 0
                })
