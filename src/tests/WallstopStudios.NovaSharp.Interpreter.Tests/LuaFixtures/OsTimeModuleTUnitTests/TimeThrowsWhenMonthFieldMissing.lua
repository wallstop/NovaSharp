-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/OsTimeModuleTUnitTests.cs:61
-- @test: OsTimeModuleTUnitTests.TimeThrowsWhenMonthFieldMissing
-- @compat-notes: Lua 5.3+: bitwise operators
return os.time({
                        year = 1985,
                        day = 12
                    })
