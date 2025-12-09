-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:928
-- @test: DebugModuleTUnitTests.GetLocalFromFunctionReturnsNilForZeroOrNegativeIndex
local function sample() end
                return debug.getlocal(sample, -1)
