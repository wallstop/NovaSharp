-- @lua-versions: 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:1748
-- @test: DebugModuleTUnitTests.UpvalueIdThrowsForOutOfRangeIndexPreLua54
-- @compat-notes: Test targets Lua 5.1; Lua 5.2+: _ENV variable; Lua 5.2+: debug.upvalueid (5.2+)
local function f()
                        -- Has _ENV as upvalue but nothing else
                    end
                    return debug.upvalueid(f, 999)
