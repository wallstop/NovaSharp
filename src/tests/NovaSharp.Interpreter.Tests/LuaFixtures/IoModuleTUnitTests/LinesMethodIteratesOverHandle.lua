-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleTUnitTests.cs:966
-- @test: IoModuleTUnitTests.LinesMethodIteratesOverHandle
-- @compat-notes: Lua 5.3+: bitwise operators
local f = assert(io.open('{escapedPath}', 'r'))
                    local out = {{}}
                    for line in f:lines() do
                        out[#out + 1] = line
                    end
                    return out[1], out[2], out[3], io.Type(f)
