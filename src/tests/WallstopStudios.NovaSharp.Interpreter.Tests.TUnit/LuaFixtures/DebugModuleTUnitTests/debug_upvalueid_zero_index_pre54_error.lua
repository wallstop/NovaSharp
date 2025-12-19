-- @lua-versions: 5.2, 5.3
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs
-- @test: DebugModuleTUnitTests.UpvalueIdThrowsForZeroIndexPreLua54

-- Test: debug.upvalueid throws error for zero index in Lua 5.2/5.3
-- Reference: Lua 5.2/5.3 manual - debug.upvalueid
-- @compat-notes: Lua uses 1-based indexing; zero is invalid

local x = 10
local function f() return x end
return debug.upvalueid(f, 0)
