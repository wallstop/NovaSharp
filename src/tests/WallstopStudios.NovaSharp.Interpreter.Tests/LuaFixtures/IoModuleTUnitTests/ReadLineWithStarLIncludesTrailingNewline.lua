-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\IoModuleTUnitTests.cs:232
-- @test: IoModuleTUnitTests.ReadLineWithStarLIncludesTrailingNewline
-- @compat-notes: NovaSharp: unresolved C# interpolation placeholder
local f = assert(io.open('{escapedPath}', 'r'))
                    local first = f:read('*L')
                    local second = f:read('*L')
                    f:close()
                    return first, second
