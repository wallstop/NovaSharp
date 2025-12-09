-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\DebugModuleTUnitTests.cs:1266
-- @test: DebugModuleTUnitTests.UpvalueIdReturnsUserDataForValidUpvalue
-- @compat-notes: Lua 5.3+: bitwise operators; Lua 5.2+: debug.upvalueid (5.2+)
local x = 10
                local function f()
                    return x
                end
                local id = debug.upvalueid(f, 1)
                return type(id)
