-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTapParityTUnitTests.cs:328
-- @test: DebugModuleTapParityTUnitTests.Unknown
-- @compat-notes: Lua 5.3+: bitwise operators
local function make()
                    local captured = 1
                    return function()
                        captured = captured + 1
                        return captured
                    end
                end
                local fn = make()
                local first = debug.upvalueid(fn, 2)
                local second = debug.upvalueid(fn, 2)
                return type(first), first == second
