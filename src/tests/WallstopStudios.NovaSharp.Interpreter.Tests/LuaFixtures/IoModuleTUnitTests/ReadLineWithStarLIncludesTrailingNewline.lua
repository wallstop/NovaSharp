-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleTUnitTests.cs:301
-- @test: IoModuleTUnitTests.ReadLineWithStarLIncludesTrailingNewline
-- @compat-notes: NovaSharp: unresolved C# interpolation placeholder; Test targets Lua 5.1
local f = assert(io.open('{escapedPath}', 'r'))
                    local first = f:read('*L')
                    local second = f:read('*L')
                    f:close()
                    return first, second
