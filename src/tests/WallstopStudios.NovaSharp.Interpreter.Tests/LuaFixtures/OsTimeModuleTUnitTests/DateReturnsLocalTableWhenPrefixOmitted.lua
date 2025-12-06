-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/OsTimeModuleTUnitTests.cs:174
-- @test: OsTimeModuleTUnitTests.DateReturnsLocalTableWhenPrefixOmitted
return os.date('*t', 1609459200)
