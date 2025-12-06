-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:189
-- @test: DebugModuleTUnitTests.GetUpvalueReturnsNilForInvalidIndex
local function f() end
                return debug.getupvalue(f, 999)
