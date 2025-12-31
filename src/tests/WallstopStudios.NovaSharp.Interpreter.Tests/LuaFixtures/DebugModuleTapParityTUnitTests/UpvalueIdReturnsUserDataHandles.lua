-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTapParityTUnitTests.cs:347
-- @test: DebugModuleTapParityTUnitTests.UpvalueIdReturnsUserDataHandles
-- @compat-notes: Lua 5.2+: debug.upvalueid (5.2+)
local function make()
                    local captured = 1
                    return function()
                        captured = captured + 1
                        return captured
                    end
                end
                local fn = make()
                local first = debug.upvalueid(fn, 1)
                local second = debug.upvalueid(fn, 1)
                return type(first), first == second
