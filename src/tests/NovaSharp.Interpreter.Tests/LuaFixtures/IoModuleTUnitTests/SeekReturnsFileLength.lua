-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleTUnitTests.cs:319
-- @test: IoModuleTUnitTests.SeekReturnsFileLength
-- @compat-notes: Lua 5.3+: bitwise operators
local f = assert(io.open('{escapedPath}', 'r'))
                    local size = f:seek('end', 0)
                    f:close()
                    return size
