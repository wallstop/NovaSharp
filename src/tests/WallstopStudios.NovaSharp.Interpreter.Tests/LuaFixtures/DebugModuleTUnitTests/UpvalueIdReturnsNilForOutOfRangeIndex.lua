-- @lua-versions: 5.2+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:1229
-- @test: DebugModuleTUnitTests.UpvalueIdReturnsNilForOutOfRangeIndex
-- Lua 5.2+: _ENV variable; Lua 5.2+: debug.upvalueid (5.2+)
local function f()
                    -- Has _ENV as upvalue but nothing else
                end
                return debug.upvalueid(f, 999)
