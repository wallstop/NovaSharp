-- @lua-versions: 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:1775
-- @test: DebugModuleTUnitTests.UpvalueIdReturnsUserDataForValidUpvalue
-- @compat-notes: Test targets Lua 5.1; Lua 5.2+: debug.upvalueid (5.2+)
local x = 10
                local function f()
                    return x
                end
                local id = debug.upvalueid(f, 1)
                return type(id)
