-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ScriptCallTUnitTests.cs:3463
-- @test: ScriptCallTUnitTests.FourDynValueCallRejectsFourthValueOwnedByDifferentScript
-- Compatibility notes: Test targets Lua 5.1
function echo(a, b, c, d) return d end
