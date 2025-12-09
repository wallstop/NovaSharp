-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/OsTimeModuleTUnitTests.cs:193
-- @test: OsTimeModuleTUnitTests.DateFormatsWeekPatterns
return os.date('!%U-%W-%V', 0)
