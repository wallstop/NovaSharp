-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\OsTimeModuleTUnitTests.cs:363
-- @test: OsTimeModuleTUnitTests.TimeIgnoresNonNumericOptionalFieldsInLua52
-- @compat-notes: Test targets Lua 5.2+; Lua 5.3+: bitwise operators
return os.time({ year = 1970, month = 1, day = 1, hour = 'ignored' })
