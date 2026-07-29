-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTapParityTUnitTests.cs:286
-- @test: DebugModuleTapParityTUnitTests.SetupValueUpdatesClosure
-- Compatibility notes: NovaSharp: unresolved C# interpolation placeholder; Test targets Lua 5.1
local function make()
                    local captured = 1
                    local function inner()
                        return captured
                    end
                    return inner
                end
                local fn = make()
                debug.setupvalue(fn, {capturedIndex}, 42)
                return fn()
