-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:896
-- @test: DebugModuleTUnitTests.GetLocalFromFunctionReturnsUpValuePlaceholder
-- @compat-notes: Lua 5.3+: bitwise operators
local x = 10
                local function closure()
                    return x
                end
                return debug.getlocal(closure, 1)
