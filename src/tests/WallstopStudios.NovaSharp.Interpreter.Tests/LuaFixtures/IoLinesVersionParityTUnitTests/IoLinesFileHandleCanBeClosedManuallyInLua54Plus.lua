-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/IoLinesVersionParityTUnitTests.cs:165
-- @test: IoLinesVersionParityTUnitTests.IoLinesFileHandleCanBeClosedManuallyInLua54Plus
-- @compat-notes: NovaSharp: unresolved C# interpolation placeholder; Test targets Lua 5.4+
local iter, a, b, fh = io.lines('{path}')
                local typeBeforeClose = io.type(fh)
                fh:close()
                local typeAfterClose = io.type(fh)
                return typeBeforeClose, typeAfterClose
