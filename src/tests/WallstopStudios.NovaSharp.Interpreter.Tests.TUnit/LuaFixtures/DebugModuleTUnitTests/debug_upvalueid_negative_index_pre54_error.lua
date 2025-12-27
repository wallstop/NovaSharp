-- @lua-versions: 5.2, 5.3
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs
-- @test: DebugModuleTUnitTests.UpvalueIdThrowsForNegativeIndexPreLua54

-- Test: debug.upvalueid throws error for negative index in Lua 5.2/5.3
-- Reference: Lua 5.2/5.3 manual - debug.upvalueid
-- @compat-notes: Negative indices are invalid

local x = 10
local function f() return x end
return debug.upvalueid(f, -1)
