-- @lua-versions: none
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:437
-- @test: DebugModuleTUnitTests.UpvalueIdThrowsForNegativeIndexPreLua54
-- @compat-notes: Test targets Lua 5.1; Lua 5.2+: debug.upvalueid (5.2+)
local x = 10
                    local function f() return x end
                    return debug.upvalueid(f, -1)
