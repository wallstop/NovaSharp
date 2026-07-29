-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/DataTypes/TailCallTUnitTests.cs:291
-- @test: TailCallTUnitTests.DebugGetInfoFunctionTargetReportsFalseForTailCallFlag
-- Compatibility notes: Test targets Lua 5.1
local function target()
                end

                return debug.getinfo(target, 't').istailcall
