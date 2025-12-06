-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/OsTimeModuleTUnitTests.cs:80
-- @test: OsTimeModuleTUnitTests.TimeThrowsWhenYearFieldMissing
-- @compat-notes: Lua 5.3+: bitwise operators
return os.time({
                        month = 5,
                        day = 12
                    })
