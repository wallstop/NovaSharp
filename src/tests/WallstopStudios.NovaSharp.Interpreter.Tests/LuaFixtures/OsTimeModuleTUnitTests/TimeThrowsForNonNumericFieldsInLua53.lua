-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/OsTimeModuleTUnitTests.cs:545
-- @test: OsTimeModuleTUnitTests.TimeThrowsForNonNumericFieldsInLua53
-- @compat-notes: Test targets Lua 5.1
return os.time({ year = 1970, month = 1, day = 1, hour = 'ignored' })
