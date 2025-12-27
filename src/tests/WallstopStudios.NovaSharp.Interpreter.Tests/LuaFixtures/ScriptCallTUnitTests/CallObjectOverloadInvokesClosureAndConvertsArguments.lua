-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ScriptExecution/ScriptCallTUnitTests.cs:156
-- @test: ScriptCallTUnitTests.CallObjectOverloadInvokesClosureAndConvertsArguments
-- @compat-notes: Test targets Lua 5.1
function mul(a, b) return a * b end
