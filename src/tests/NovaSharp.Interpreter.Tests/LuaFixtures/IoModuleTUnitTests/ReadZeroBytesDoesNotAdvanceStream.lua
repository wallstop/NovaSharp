-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleTUnitTests.cs:233
-- @test: IoModuleTUnitTests.ReadZeroBytesDoesNotAdvanceStream
-- @compat-notes: Lua 5.3+: bitwise operators
local f = assert(io.open('{escapedPath}', 'r'))
                    local zero = f:read(0)
                    local chunk = f:read(3)
                    local remainder = f:read('*a')
                    f:close()
                    return zero, chunk, remainder
