-- @lua-versions: 5.2+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs
-- @test: DebugModuleTUnitTests.UpvalueIdReturnsUserDataForValidUpvalue

-- Test: debug.upvalueid returns userdata for valid upvalue
-- Reference: Lua 5.2+ manual - debug.upvalueid
-- Returns unique identifier for upvalue reference

local x = 10
local function f() return x end
local id = debug.upvalueid(f, 1)
print("type:", type(id))
return type(id) == "userdata"
