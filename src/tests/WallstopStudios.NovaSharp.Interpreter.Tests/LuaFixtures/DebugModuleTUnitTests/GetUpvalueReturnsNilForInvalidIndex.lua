-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\DebugModuleTUnitTests.cs:190
-- @test: DebugModuleTUnitTests.GetUpvalueReturnsNilForInvalidIndex
local function f() end
                return debug.getupvalue(f, 999)
