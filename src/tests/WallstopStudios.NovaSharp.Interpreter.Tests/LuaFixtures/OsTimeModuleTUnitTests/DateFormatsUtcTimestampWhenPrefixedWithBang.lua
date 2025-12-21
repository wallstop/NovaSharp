-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/OsTimeModuleTUnitTests.cs:215
-- @test: OsTimeModuleTUnitTests.DateFormatsUtcTimestampWhenPrefixedWithBang
-- @compat-notes: Test targets Lua 5.1
return os.date('!%Y-%m-%d %H:%M:%S', 1609459200)
