-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/OsSystemModuleTUnitTests.cs:78
-- @test: OsSystemModuleTUnitTests.ExecuteCommandReportsSignalWhenExitCodeNegative
return os.execute('terminate')
