-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\DebugModuleTapParityTUnitTests.cs:263
-- @test: DebugModuleTapParityTUnitTests.SetupValueUpdatesClosure
-- @compat-notes: Lua 5.3+: bitwise operators
local function make()
                    local captured = 1
                    local function inner()
                        return captured
                    end
                    return inner
                end
                local fn = make()
                debug.setupvalue(fn, 2, 42)
                return fn()
