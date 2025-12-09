-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/OsTimeModuleTUnitTests.cs:81
-- @test: OsTimeModuleTUnitTests.TimeThrowsWhenYearFieldMissing
-- @compat-notes: Lua 5.3+: bitwise operators
return os.time({
                        month = 5,
                        day = 12
                    })
