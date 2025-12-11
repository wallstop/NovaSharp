-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\IoModuleVirtualizationTUnitTests.cs:74
-- @test: IoModuleVirtualizationTUnitTests.OsRemoveDeletesVirtualFile
-- @compat-notes: Test class 'IoModuleVirtualizationTUnitTests' uses NovaSharp-specific IoModuleVirtualization functionality
local f = io.open('temp.txt', 'w'); f:write('payload'); f:close()
