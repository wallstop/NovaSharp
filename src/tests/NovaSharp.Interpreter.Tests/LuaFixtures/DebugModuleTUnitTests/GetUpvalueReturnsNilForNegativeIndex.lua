-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:204
-- @test: DebugModuleTUnitTests.GetUpvalueReturnsNilForNegativeIndex
-- @compat-notes: Lua 5.3+: bitwise operators
local x = 10
                local function f() return x end
                return debug.getupvalue(f, -1)
