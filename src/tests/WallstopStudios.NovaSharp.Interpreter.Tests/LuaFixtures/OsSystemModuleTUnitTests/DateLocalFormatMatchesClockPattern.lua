-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/OsSystemModuleTUnitTests.cs:319
-- @test: OsSystemModuleTUnitTests.DateLocalFormatMatchesClockPattern
-- @compat-notes: Test class 'OsSystemModuleTUnitTests' uses NovaSharp-specific OsSystemModule functionality
return os.date('%H:%M:%S')
