-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/OsSystemModuleTUnitTests.cs:306
-- @test: OsSystemModuleTUnitTests.DifftimeReturnsDelta
return os.difftime(1234, 1200)
