-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/OsSystemModuleTUnitTests.cs:427
-- @test: OsSystemModuleTUnitTests.DatePercentOyYieldsTwoDigitYear
return os.date('!%Oy', 0)
