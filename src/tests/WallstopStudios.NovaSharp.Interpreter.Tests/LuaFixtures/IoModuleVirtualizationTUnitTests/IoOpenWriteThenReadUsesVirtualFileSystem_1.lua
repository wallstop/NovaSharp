-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleVirtualizationTUnitTests.cs:29
-- @test: IoModuleVirtualizationTUnitTests.IoOpenWriteThenReadUsesVirtualFileSystem
-- @compat-notes: Test class 'IoModuleVirtualizationTUnitTests' uses NovaSharp-specific IoModuleVirtualization functionality
local f = io.open('virtual.txt', 'r')
                local data = f:read('*a')
                f:close()
                return data
