-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/OsSystemModuleTUnitTests.cs:93
-- @test: OsSystemModuleTUnitTests.ExecuteCommandReportsNotSupportedMessage
return os.execute('build')
