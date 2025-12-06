-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/OsTimeModuleTUnitTests.cs:256
-- @test: OsTimeModuleTUnitTests.TimeDefaultsHourToNoonWhenFieldsOmitted
-- @compat-notes: Lua 5.3+: bitwise operators
return os.time({ year = 1970, month = 1, day = 1 })
