-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/OsSystemModuleTUnitTests.cs:294
-- @test: OsSystemModuleTUnitTests.DateLocalFormatMatchesClockPattern
return os.date('%H:%M:%S')
