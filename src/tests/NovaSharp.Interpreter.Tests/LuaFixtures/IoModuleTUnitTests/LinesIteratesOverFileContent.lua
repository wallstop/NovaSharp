-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleTUnitTests.cs:668
-- @test: IoModuleTUnitTests.LinesIteratesOverFileContent
-- @compat-notes: Lua 5.3+: bitwise operators
local iter = io.lines('{escapedPath}')
                    return iter(), iter(), iter(), iter()
