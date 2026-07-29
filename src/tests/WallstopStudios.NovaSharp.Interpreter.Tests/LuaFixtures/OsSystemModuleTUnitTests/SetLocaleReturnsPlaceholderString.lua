-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/OsSystemModuleTUnitTests.cs:585
-- @test: OsSystemModuleTUnitTests.SetLocaleReturnsPlaceholderString
-- Compatibility notes: Test class 'OsSystemModuleTUnitTests' uses NovaSharp-specific OsSystemModule functionality
return os.setlocale()
