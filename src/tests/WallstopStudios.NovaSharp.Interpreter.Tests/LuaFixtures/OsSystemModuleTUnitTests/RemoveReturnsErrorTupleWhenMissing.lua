-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\OsSystemModuleTUnitTests.cs:187
-- @test: OsSystemModuleTUnitTests.RemoveReturnsErrorTupleWhenMissing
-- Test class 'OsSystemModuleTUnitTests' uses NovaSharp-specific OsSystemModule functionality
return os.remove('missing.txt')
