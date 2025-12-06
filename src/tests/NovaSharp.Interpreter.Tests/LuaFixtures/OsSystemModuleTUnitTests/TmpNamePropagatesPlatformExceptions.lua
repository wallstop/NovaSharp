-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/OsSystemModuleTUnitTests.cs:482
-- @test: OsSystemModuleTUnitTests.TmpNamePropagatesPlatformExceptions
return os.tmpname()
