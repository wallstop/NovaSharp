-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/OsTimeModuleTUnitTests.cs:150
-- @test: OsTimeModuleTUnitTests.DateFormatsUtcTimestampWhenPrefixedWithBang
return os.date('!%Y-%m-%d %H:%M:%S', 1609459200)
