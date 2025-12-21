-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleVirtualizationTUnitTests.cs:61
-- @test: IoModuleVirtualizationTUnitTests.IoOutputRedirectWritesToVirtualFile
-- @compat-notes: Test class 'IoModuleVirtualizationTUnitTests' uses NovaSharp-specific IoModuleVirtualization functionality
local previous = io.output()
                local redirected = io.output('log.txt')
                io.write('abc')
                io.write('123')
                io.flush()
                io.flush()
                redirected:close()
                io.output(previous)
