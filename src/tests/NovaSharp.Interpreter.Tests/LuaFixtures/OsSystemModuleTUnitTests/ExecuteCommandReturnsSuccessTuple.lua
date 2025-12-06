-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/OsSystemModuleTUnitTests.cs:32
-- @test: OsSystemModuleTUnitTests.ExecuteCommandReturnsSuccessTuple
return os.execute('build')
