-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/OsSystemModuleTUnitTests.cs:117
-- @test: OsSystemModuleTUnitTests.ExecuteWithoutArgumentsReturnsTrue
-- Compatibility notes: Test class 'OsSystemModuleTUnitTests' uses NovaSharp-specific OsSystemModule functionality
return os.execute()
