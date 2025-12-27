-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/OsTimeModuleTUnitTests.cs:101
-- @test: OsTimeModuleTUnitTests.TimeThrowsWhenYearFieldMissing
-- @compat-notes: Test targets Lua 5.1
return os.time({
                        month = 5,
                        day = 12
                    })
