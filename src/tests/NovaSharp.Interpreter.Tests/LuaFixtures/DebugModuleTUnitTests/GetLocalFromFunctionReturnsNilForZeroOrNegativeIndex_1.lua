-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:928
-- @test: DebugModuleTUnitTests.GetLocalFromFunctionReturnsNilForZeroOrNegativeIndex
local function sample() end
                return debug.getlocal(sample, -1)
