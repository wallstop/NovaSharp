-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/OsSystemModuleTUnitTests.cs:63
-- @test: OsSystemModuleTUnitTests.ExecuteCommandReturnsNilTupleOnNonZeroExit
return os.execute('fail')
