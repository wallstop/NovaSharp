-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:2170
-- @test: DebugModuleTUnitTests.UpvalueIdReturnsUserDataForValidUpvalue
-- Lua 5.2+: debug.upvalueid
local x = 10
                local function f()
                    return x
                end
                return debug.upvalueid(f, 1)
