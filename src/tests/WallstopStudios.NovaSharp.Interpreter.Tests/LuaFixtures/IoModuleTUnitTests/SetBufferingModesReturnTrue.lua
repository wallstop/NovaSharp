-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleTUnitTests.cs:1070
-- @test: IoModuleTUnitTests.SetBufferingModesReturnTrue
-- @compat-notes: NovaSharp: unresolved C# interpolation placeholder; Test targets Lua 5.1
local f = assert(io.open('{escapedPath}', 'w'))
                    local noop = f:setvbuf('no')
                    local full = f:setvbuf('full', 128)
                    local line = f:setvbuf('line', 64)
                    f:close()
                    return noop, full, line
