-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/OsSystemModuleTUnitTests.cs:215
-- @test: OsSystemModuleTUnitTests.RenameReturnsTupleWhenSourceMissing
return os.rename('nope', 'dest')
