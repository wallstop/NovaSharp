-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleTUnitTests.cs:435
-- @test: IoModuleTUnitTests.SeekReturnsFileLength
-- @compat-notes: NovaSharp: unresolved C# interpolation placeholder; Test targets Lua 5.1
local f = assert(io.open('{escapedPath}', 'r'))
                    local size = f:seek('end', 0)
                    f:close()
                    return size
