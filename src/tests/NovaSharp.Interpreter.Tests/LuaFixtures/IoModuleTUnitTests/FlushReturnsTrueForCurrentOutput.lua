-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleTUnitTests.cs:627
-- @test: IoModuleTUnitTests.FlushReturnsTrueForCurrentOutput
-- @compat-notes: Lua 5.3+: bitwise operators
local f = assert(io.open('{escapedPath}', 'w'))
                    io.output(f)
                    io.write('buffered')
                    local ok = io.flush()
                    io.output():close()
                    return ok
