-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs
-- @test: DebugModuleTUnitTests.UpvalueIdReturnsNilForInvalidIndexLua54Plus

-- Test: debug.upvalueid returns nil for invalid indices in Lua 5.4+
-- Reference: Lua 5.4 manual - debug.upvalueid

local function f() end
-- Index 999 is far beyond available upvalues
local result = debug.upvalueid(f, 999)
print(result == nil)
return result == nil
