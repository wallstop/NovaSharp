-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/OsTimeModuleTUnitTests.cs:234
-- @test: OsTimeModuleTUnitTests.DateThrowsWhenConversionSpecifierUnknown
return os.date('%Q', 1609459200)
