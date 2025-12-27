-- @lua-versions: 5.1+
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/OsTimeModuleTUnitTests.cs:234
-- @test: OsTimeModuleTUnitTests.DateThrowsWhenConversionSpecifierUnknown
-- @compat-notes: Windows Lua crashes when strftime encounters unknown format specifiers like %Q. NovaSharp handles these gracefully.
return os.date('%Q', 1609459200)