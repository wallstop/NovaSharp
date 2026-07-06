-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/DataTypes/TailCallTUnitTests.cs:173
-- @test: TailCallTUnitTests.DebugGetInfoDefaultIncludesTailCallFlag
-- Compatibility notes: Test targets Lua 5.2+
local function target()
                    return debug.getinfo(1).istailcall
                end

                local function caller()
                    return target()
                end

                return caller()
