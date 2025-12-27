-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleVirtualizationTUnitTests.cs:33
-- @test: IoModuleVirtualizationTUnitTests.IoOpenWriteThenReadUsesVirtualFileSystem
-- @compat-notes: Test class 'IoModuleVirtualizationTUnitTests' uses NovaSharp-specific IoModuleVirtualization functionality
local f = io.open('virtual.txt', 'w'); f:write('hello'); f:close()
