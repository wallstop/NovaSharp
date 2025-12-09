-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:919
-- @test: DebugModuleTUnitTests.GetLocalFromFunctionReturnsNilForZeroOrNegativeIndex
local function sample() end
                return debug.getlocal(sample, 0)
