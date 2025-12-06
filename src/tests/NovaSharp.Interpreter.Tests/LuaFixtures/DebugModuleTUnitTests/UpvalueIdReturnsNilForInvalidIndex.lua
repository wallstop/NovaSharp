-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:230
-- @test: DebugModuleTUnitTests.UpvalueIdReturnsNilForInvalidIndex
local function f() end
                return debug.upvalueid(f, 999)
