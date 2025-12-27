-- @lua-versions: 5.1, 5.2, 5.3
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/IoLinesVersionParityTUnitTests.cs
-- @test: IoLinesVersionParityTUnitTests.IoLinesReturnsThreeValuesInLua51To53
-- @compat-notes: Lua 5.1-5.3 io.lines returns 3 values (iterator, nil, nil)

-- Test: io.lines returns 3 values in Lua 5.1-5.3
-- Reference: Lua 5.3 Reference Manual §6.8

-- Create a temp file
local tmpname = os.tmpname()
local f = io.open(tmpname, "w")
f:write("line1\nline2\nline3\n")
f:close()

local a, b, c, d = io.lines(tmpname)

local result = {
    iter_is_callable = type(a) == "function",
    second_is_nil = b == nil,
    third_is_nil = c == nil,
    fourth_is_nil = d == nil
}

os.remove(tmpname)

if result.iter_is_callable and result.second_is_nil and result.third_is_nil and result.fourth_is_nil then
    print("PASS")
else
    print("FAIL: iter_is_callable=" .. tostring(result.iter_is_callable) ..
          ", second_is_nil=" .. tostring(result.second_is_nil) ..
          ", third_is_nil=" .. tostring(result.third_is_nil) ..
          ", fourth_is_nil=" .. tostring(result.fourth_is_nil))
end
