-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/OsTimeModuleTUnitTests.cs:379
-- @test: OsTimeModuleTUnitTests.DateReturnsLiteralForUnknownSpecifierInLua51
-- @compat-notes: Windows Lua crashes when strftime encounters unknown format specifiers like %Q. NovaSharp handles these gracefully.
return os.date('%Q', 1609459200)