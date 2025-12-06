-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/OsTimeModuleTUnitTests.cs:192
-- @test: OsTimeModuleTUnitTests.DateFormatsWeekPatterns
return os.date('!%U-%W-%V', 0)
