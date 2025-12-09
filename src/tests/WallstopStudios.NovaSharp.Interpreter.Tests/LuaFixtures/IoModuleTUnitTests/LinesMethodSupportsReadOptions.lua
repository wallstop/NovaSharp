-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleTUnitTests.cs:1010
-- @test: IoModuleTUnitTests.LinesMethodSupportsReadOptions
-- @compat-notes: NovaSharp: unresolved C# interpolation placeholder
local f = assert(io.open('{escapedPath}', 'r'))
                    local chunks = {{}}
                    for chunk in f:lines(2) do
                        chunks[#chunks + 1] = chunk
                        if #chunks == 3 then break end
                    end
                    f:close()
                    return chunks[1], chunks[2], chunks[3]
