-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/OsExecuteVersionParityTUnitTests.cs:58
-- @test: OsExecuteVersionParityTUnitTests.OsExecuteReturnsNonZeroNumberOnFailureInLua51
-- @compat-notes: Test class 'OsExecuteVersionParityTUnitTests' uses NovaSharp-specific OsExecuteVersionParity functionality
return os.execute('fail')
