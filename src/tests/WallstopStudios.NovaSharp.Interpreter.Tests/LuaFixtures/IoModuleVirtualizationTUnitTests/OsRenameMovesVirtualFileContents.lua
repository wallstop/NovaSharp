-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\IoModuleVirtualizationTUnitTests.cs:115
-- @test: IoModuleVirtualizationTUnitTests.OsRenameMovesVirtualFileContents
-- @compat-notes: Test class 'IoModuleVirtualizationTUnitTests' uses NovaSharp-specific IoModuleVirtualization functionality
local f = io.open('old.txt', 'w'); f:write('payload'); f:close()
