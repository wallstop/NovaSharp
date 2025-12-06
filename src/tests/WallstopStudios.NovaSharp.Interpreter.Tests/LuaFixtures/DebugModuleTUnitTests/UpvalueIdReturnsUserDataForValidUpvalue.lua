-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:1248
-- @test: DebugModuleTUnitTests.UpvalueIdReturnsUserDataForValidUpvalue
-- @compat-notes: Lua 5.3+: bitwise operators
local x = 10
                local function f()
                    return x
                end
                local id = debug.upvalueid(f, 1)
                return type(id)
