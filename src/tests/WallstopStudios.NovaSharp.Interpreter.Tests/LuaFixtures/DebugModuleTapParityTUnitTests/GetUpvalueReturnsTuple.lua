-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTapParityTUnitTests.cs:259
-- @test: DebugModuleTapParityTUnitTests.GetUpvalueReturnsTuple
-- Compatibility notes: NovaSharp: unresolved C# interpolation placeholder; Test targets Lua 5.1
local function make()
                    local captured = 7
                    local function inner()
                        return captured
                    end
                    return inner
                end
                local fn = make()
                return debug.getupvalue(fn, {capturedIndex})
