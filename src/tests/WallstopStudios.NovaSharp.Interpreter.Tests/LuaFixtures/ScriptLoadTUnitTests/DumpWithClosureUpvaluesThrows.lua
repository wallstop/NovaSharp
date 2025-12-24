-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ScriptLoadTUnitTests.cs:280
-- @test: ScriptLoadTUnitTests.DumpWithClosureUpvaluesThrows
-- @compat-notes: Test targets Lua 5.1
local captured = 10
                withCapture = function() return captured end
