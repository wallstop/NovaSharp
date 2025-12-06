-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleVirtualizationTUnitTests.cs:50
-- @test: IoModuleVirtualizationTUnitTests.IoOutputRedirectWritesToVirtualFile
-- @compat-notes: Lua 5.3+: bitwise operators
local previous = io.output()
                local redirected = io.output('log.txt')
                io.write('abc')
                io.write('123')
                io.flush()
                io.flush()
                redirected:close()
                io.output(previous)
