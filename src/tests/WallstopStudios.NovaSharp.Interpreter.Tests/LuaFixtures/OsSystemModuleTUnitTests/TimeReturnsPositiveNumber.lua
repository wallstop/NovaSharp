-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/OsSystemModuleTUnitTests.cs:328
-- @test: OsSystemModuleTUnitTests.TimeReturnsPositiveNumber
return os.time()
