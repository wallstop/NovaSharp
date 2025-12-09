-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ScriptLoadTUnitTests.cs:207
-- @test: ScriptLoadTUnitTests.DumpWithClosureUpvaluesThrows
-- @compat-notes: Lua 5.3+: bitwise operators
local captured = 10
                withCapture = function() return captured end
