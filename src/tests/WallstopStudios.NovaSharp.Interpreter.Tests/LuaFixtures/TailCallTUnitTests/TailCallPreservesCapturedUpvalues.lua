-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/DataTypes/TailCallTUnitTests.cs:343
-- @test: TailCallTUnitTests.TailCallPreservesCapturedUpvalues
-- Compatibility notes: Test targets Lua 5.1
local saved

                local function finish()
                    return saved()
                end

                local function caller(value)
                    local captured = value
                    saved = function()
                        return captured
                    end

                    return finish()
                end

                return caller(91)
