-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/OsSystemModuleTUnitTests.cs:119
-- @test: OsSystemModuleTUnitTests.ExitInvokesPlatformAndThrowsExitException
os.exit(5)
