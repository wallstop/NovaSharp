-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ScriptLoadTUnitTests.cs:2507
-- @test: ScriptLoadTUnitTests.DumpWithClosureUpvaluesThrows
-- Compatibility notes: Test targets Lua 5.1
local captured = 10
                withCapture = function() return captured end
