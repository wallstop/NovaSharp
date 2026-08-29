-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ScriptCallTUnitTests.cs:3224
-- @test: ScriptCallTUnitTests.CallObjectOverloadInvokesClosureAndConvertsArguments
-- Compatibility notes: Test targets Lua 5.1
function mul(a, b, c, d) return a * b + c + d end
