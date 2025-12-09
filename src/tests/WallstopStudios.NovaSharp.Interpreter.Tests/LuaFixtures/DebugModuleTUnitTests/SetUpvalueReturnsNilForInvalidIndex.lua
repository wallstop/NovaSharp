-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:269
-- @test: DebugModuleTUnitTests.SetUpvalueReturnsNilForInvalidIndex
local function f() end
                return debug.setupvalue(f, 999, 'test')
