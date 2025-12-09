-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/OsSystemModuleTUnitTests.cs:144
-- @test: OsSystemModuleTUnitTests.GetEnvReturnsNilWhenMissing
return os.getenv('MISSING')
