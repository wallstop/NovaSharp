-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/OsExecuteVersionParityTUnitTests.cs:219
-- @test: OsExecuteVersionParityTUnitTests.OsExecuteWithNoArgsReturnsTrueAllVersions
-- @compat-notes: Test class 'OsExecuteVersionParityTUnitTests' uses NovaSharp-specific OsExecuteVersionParity functionality
return os.execute()
