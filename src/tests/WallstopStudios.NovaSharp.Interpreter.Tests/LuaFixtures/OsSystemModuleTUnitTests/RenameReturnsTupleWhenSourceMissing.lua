-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\OsSystemModuleTUnitTests.cs:235
-- @test: OsSystemModuleTUnitTests.RenameReturnsTupleWhenSourceMissing
-- Test class 'OsSystemModuleTUnitTests' uses NovaSharp-specific OsSystemModule functionality
return os.rename('nope', 'dest')
