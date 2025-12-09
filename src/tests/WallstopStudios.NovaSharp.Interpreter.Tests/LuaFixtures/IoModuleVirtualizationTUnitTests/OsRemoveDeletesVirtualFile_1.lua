-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleVirtualizationTUnitTests.cs:77
-- @test: IoModuleVirtualizationTUnitTests.OsRemoveDeletesVirtualFile
return os.remove('temp.txt')
