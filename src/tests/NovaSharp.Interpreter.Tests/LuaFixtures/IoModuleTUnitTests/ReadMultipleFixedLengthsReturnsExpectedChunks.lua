-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleTUnitTests.cs:256
-- @test: IoModuleTUnitTests.ReadMultipleFixedLengthsReturnsExpectedChunks
-- @compat-notes: Lua 5.3+: bitwise operators
local f = assert(io.open('{escapedPath}', 'r'))
                    local first, second, third = f:read(4, 4, 4)
                    f:close()
                    return first, second, third
