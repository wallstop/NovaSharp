-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/OsTimeModuleTUnitTests.cs:53
-- @test: OsTimeModuleTUnitTests.TimeThrowsWhenDayFieldMissing
-- @compat-notes: Test targets Lua 5.1
return os.time({
                        year = 1985,
                        month = 5
                    })
