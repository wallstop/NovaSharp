-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleTUnitTests.cs:327
-- @test: IoModuleTUnitTests.ReadZeroBytesDoesNotAdvanceStream
-- @compat-notes: NovaSharp: unresolved C# interpolation placeholder; Test targets Lua 5.1
local f = assert(io.open('{escapedPath}', 'r'))
                    local zero = f:read(0)
                    local chunk = f:read(3)
                    local remainder = f:read('*a')
                    f:close()
                    return zero, chunk, remainder
