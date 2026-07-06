-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/DataTypes/TailCallTUnitTests.cs:246
-- @test: TailCallTUnitTests.DebugGetInfoDefaultReportsClrFrameAsNotTailCalled
-- Compatibility notes: Test targets Lua 5.2+
local function caller()
                    return debug.getinfo(0).istailcall
                end

                return caller()
