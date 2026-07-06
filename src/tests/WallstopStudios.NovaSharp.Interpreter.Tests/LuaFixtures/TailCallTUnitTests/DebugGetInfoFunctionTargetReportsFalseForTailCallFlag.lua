-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/DataTypes/TailCallTUnitTests.cs:291
-- @test: TailCallTUnitTests.DebugGetInfoFunctionTargetReportsFalseForTailCallFlag
-- Compatibility notes: Test targets Lua 5.2+
local function target()
end

local info = debug.getinfo(target, 't')
assert(info.istailcall == false)
