-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleTUnitTests.cs:990
-- @test: IoModuleTUnitTests.LinesMethodSupportsReadOptions
-- @compat-notes: Lua 5.3+: bitwise operators
local f = assert(io.open('{escapedPath}', 'r'))
                    local chunks = {{}}
                    for chunk in f:lines(2) do
                        chunks[#chunks + 1] = chunk
                        if #chunks == 3 then break end
                    end
                    f:close()
                    return chunks[1], chunks[2], chunks[3]
