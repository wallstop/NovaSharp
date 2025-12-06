-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTapParityTUnitTests.cs:240
-- @test: DebugModuleTapParityTUnitTests.Unknown
-- @compat-notes: Lua 5.3+: bitwise operators
local function make()
                    local captured = 7
                    local function inner()
                        return captured
                    end
                    return inner
                end
                local fn = make()
                return debug.getupvalue(fn, 2)
