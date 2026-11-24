-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleTUnitTests.cs:1194
-- @test: IoModuleTUnitTests.LinesIteratesOverFileContent
-- Compatibility notes: NovaSharp: unresolved C# interpolation placeholder; Test targets Lua 5.1
local iter = io.lines('{escapedPath}')
                    local first, second, third, eof = iter(), iter(), iter(), iter()
                    return first, second, third, eof
