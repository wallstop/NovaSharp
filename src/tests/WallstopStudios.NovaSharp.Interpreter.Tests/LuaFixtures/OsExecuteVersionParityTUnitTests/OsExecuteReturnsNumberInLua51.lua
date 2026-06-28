-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\OsExecuteVersionParityTUnitTests.cs:40
-- @test: OsExecuteVersionParityTUnitTests.OsExecuteReturnsNumberInLua51
-- Test class 'OsExecuteVersionParityTUnitTests' uses NovaSharp-specific OsExecuteVersionParity functionality
return os.execute('build')
