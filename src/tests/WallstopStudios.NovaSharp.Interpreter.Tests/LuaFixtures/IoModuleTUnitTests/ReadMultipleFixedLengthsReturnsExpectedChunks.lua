-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleTUnitTests.cs:357
-- @test: IoModuleTUnitTests.ReadMultipleFixedLengthsReturnsExpectedChunks
-- @compat-notes: NovaSharp: unresolved C# interpolation placeholder; Test targets Lua 5.1
local f = assert(io.open('{escapedPath}', 'r'))
                    local first, second, third = f:read(4, 4, 4)
                    f:close()
                    return first, second, third
