-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\OsSystemModuleTUnitTests.cs:553
-- @test: OsSystemModuleTUnitTests.TmpNameReturnsQueuedPlatformValuesThenFallsBack
-- Test class 'OsSystemModuleTUnitTests' uses NovaSharp-specific OsSystemModule functionality
return os.tmpname(), os.tmpname(), os.tmpname()
