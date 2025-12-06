-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/OsSystemModuleTUnitTests.cs:157
-- @test: OsSystemModuleTUnitTests.RemoveDeletesExistingFile
return os.remove('file.txt')
