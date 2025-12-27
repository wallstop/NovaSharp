-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleTUnitTests.cs:931
-- @test: IoModuleTUnitTests.InputReturnsCurrentFileWhenNoArguments
-- @compat-notes: NovaSharp: unresolved C# interpolation placeholder; Test targets Lua 5.1
local f = assert(io.open('{escapedPath}', 'r'))
                    io.input(f)
                    local current = io.input()
                    return io.type(current), io.type(f)
