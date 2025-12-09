-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleTUnitTests.cs:688
-- @test: IoModuleTUnitTests.LinesIteratesOverFileContent
-- @compat-notes: NovaSharp: unresolved C# interpolation placeholder
local iter = io.lines('{escapedPath}')
                    return iter(), iter(), iter(), iter()
