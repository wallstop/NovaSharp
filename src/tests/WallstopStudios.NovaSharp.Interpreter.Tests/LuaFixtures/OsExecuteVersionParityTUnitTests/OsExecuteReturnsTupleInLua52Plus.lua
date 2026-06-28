-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\OsExecuteVersionParityTUnitTests.cs:114
-- @test: OsExecuteVersionParityTUnitTests.OsExecuteReturnsTupleInLua52Plus
-- Test class 'OsExecuteVersionParityTUnitTests' uses NovaSharp-specific OsExecuteVersionParity functionality
return os.execute('build')
