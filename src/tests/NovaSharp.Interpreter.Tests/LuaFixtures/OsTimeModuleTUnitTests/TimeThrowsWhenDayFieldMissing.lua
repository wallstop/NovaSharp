-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/OsTimeModuleTUnitTests.cs:42
-- @test: OsTimeModuleTUnitTests.TimeThrowsWhenDayFieldMissing
-- @compat-notes: Lua 5.3+: bitwise operators
return os.time({
                        year = 1985,
                        month = 5
                    })
