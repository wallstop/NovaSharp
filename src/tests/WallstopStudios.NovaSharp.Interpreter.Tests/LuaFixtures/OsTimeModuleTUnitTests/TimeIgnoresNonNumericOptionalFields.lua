-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/OsTimeModuleTUnitTests.cs:352
-- @test: OsTimeModuleTUnitTests.TimeIgnoresNonNumericOptionalFields
-- @compat-notes: Lua 5.3+: bitwise operators
return os.time({ year = 1970, month = 1, day = 1, hour = 'ignored' })
