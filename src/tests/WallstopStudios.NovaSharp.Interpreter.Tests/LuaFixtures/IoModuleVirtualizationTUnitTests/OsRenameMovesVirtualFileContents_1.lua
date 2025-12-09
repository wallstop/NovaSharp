-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleVirtualizationTUnitTests.cs:117
-- @test: IoModuleVirtualizationTUnitTests.OsRenameMovesVirtualFileContents
return os.rename('old.txt', 'new.txt')
