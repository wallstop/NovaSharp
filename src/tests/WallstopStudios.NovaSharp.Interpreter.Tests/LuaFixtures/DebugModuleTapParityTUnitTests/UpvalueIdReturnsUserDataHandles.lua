-- @lua-versions: 5.2+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTapParityTUnitTests.cs:359
-- @test: DebugModuleTapParityTUnitTests.UpvalueIdReturnsUserDataHandles
-- Lua 5.2+: debug.upvalueid (5.2+)
local function make()
                    local captured = 1
                    return function()
                        captured = captured + 1
                        return captured
                    end
                end
                local fn = make()
                local first = debug.upvalueid(fn, {capturedIndex})
                local second = debug.upvalueid(fn, {capturedIndex})
                return type(first), first == second
