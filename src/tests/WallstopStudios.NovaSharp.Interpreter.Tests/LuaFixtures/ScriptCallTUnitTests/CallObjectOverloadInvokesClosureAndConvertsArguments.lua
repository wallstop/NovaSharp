-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Units\Execution\ScriptExecution\ScriptCallTUnitTests.cs:114
-- @test: ScriptCallTUnitTests.CallObjectOverloadInvokesClosureAndConvertsArguments
function mul(a, b) return a * b end
