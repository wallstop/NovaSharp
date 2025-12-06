-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/OsSystemModuleTUnitTests.cs:438
-- @test: OsSystemModuleTUnitTests.TimeWithNilArgumentReturnsTimestamp
return os.time(nil)
