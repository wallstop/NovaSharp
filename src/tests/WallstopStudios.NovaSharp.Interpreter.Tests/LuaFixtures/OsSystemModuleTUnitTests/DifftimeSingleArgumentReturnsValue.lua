-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/OsSystemModuleTUnitTests.cs:317
-- @test: OsSystemModuleTUnitTests.DifftimeSingleArgumentReturnsValue
return os.difftime(1234)
