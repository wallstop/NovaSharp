-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\DebugModuleTUnitTests.cs:941
-- @test: DebugModuleTUnitTests.GetLocalFromFunctionReturnsNilForZeroOrNegativeIndex
-- @compat-notes: Lua 5.2+: debug.getlocal with function var (5.2+)
local function sample() end
                return debug.getlocal(sample, -1)
