-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/DataTypes/TailCallTUnitTests.cs:196
-- @test: TailCallTUnitTests.DebugGetInfoOmitsNameForTailCalledFrames
-- Compatibility notes: Test targets Lua 5.2+
local function target()
                    local info = debug.getinfo(1, 'nSt')
                    return info.name == nil, info.namewhat, info.istailcall
                end

                local function caller()
                    return target()
                end

                return caller()
