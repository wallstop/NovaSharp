-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTapParityTUnitTests.cs:280
-- @test: DebugModuleTapParityTUnitTests.SetupValueUpdatesClosure
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
