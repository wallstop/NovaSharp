-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs
-- @test: DebugModuleTUnitTests.UpvalueIdReturnsNilForNegativeIndexLua54Plus

-- Test: debug.upvalueid returns nil for negative index in Lua 5.4+
-- Reference: Lua 5.4 manual - debug.upvalueid
-- @compat-notes: Negative indices are invalid

local x = 10
local function f() return x end
local result = debug.upvalueid(f, -1)
print(result == nil)
return result == nil
