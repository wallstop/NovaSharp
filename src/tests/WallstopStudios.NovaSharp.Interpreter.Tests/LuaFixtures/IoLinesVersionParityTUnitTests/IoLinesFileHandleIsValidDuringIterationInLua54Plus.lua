-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/IoLinesVersionParityTUnitTests.cs:193
-- @test: IoLinesVersionParityTUnitTests.IoLinesFileHandleIsValidDuringIterationInLua54Plus
-- @compat-notes: NovaSharp: unresolved C# interpolation placeholder; Test targets Lua 5.4+
local iter, a, b, fh = io.lines('{path}')
                local typesDuringIteration = {{}}
                local lineCount = 0
                for line in iter, a, b do
                    lineCount = lineCount + 1
                    typesDuringIteration[lineCount] = io.type(fh)
                    if lineCount >= 2 then break end
                end
                return typesDuringIteration[1], typesDuringIteration[2], io.type(fh)
