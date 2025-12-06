-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleTUnitTests.cs:752
-- @test: IoModuleTUnitTests.SetBufferingModesReturnTrue
-- @compat-notes: Lua 5.3+: bitwise operators
local f = assert(io.open('{escapedPath}', 'w'))
                    local noop = f:setvbuf('no')
                    local full = f:setvbuf('full', 128)
                    local line = f:setvbuf('line', 64)
                    f:close()
                    return noop, full, line
