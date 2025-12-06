-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleTUnitTests.cs:879
-- @test: IoModuleTUnitTests.OpenFileInvokesPlatformAccessorAndStillWritesToDisk
-- @compat-notes: Lua 5.3+: bitwise operators
local f = assert(io.open('{escapedPath}', 'w'))
                f:write('hooked payload')
                f:close()
