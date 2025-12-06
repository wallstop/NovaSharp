-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/OsTimeModuleTUnitTests.cs:149
-- @test: OsTimeModuleTUnitTests.DateFormatsUtcTimestampWhenPrefixedWithBang
return os.date('!%Y-%m-%d %H:%M:%S', 1609459200)
