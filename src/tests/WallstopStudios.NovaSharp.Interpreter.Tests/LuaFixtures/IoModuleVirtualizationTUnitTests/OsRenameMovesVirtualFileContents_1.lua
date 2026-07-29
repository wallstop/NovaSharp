-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\IoModuleVirtualizationTUnitTests.cs:143
-- @test: IoModuleVirtualizationTUnitTests.OsRenameMovesVirtualFileContents
-- Test class 'IoModuleVirtualizationTUnitTests' uses NovaSharp-specific IoModuleVirtualization functionality
return os.rename('old.txt', 'new.txt')
