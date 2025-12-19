-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/IoLinesVersionParityTUnitTests.cs
-- @test: IoLinesVersionParityTUnitTests.IoLinesReturnsFourValuesInLua54Plus
-- @compat-notes: Lua 5.4+ io.lines returns 4 values (iterator, nil, nil, file_handle)

-- Test: io.lines returns 4 values in Lua 5.4+
-- Reference: Lua 5.4 Reference Manual §6.8

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
    fourth_is_file = io.type(d) == "file"
}

-- Clean up the file handle if we got one
if d and io.type(d) == "file" then
    d:close()
end

os.remove(tmpname)

if result.iter_is_callable and result.second_is_nil and result.third_is_nil and result.fourth_is_file then
    print("PASS")
else
    print("FAIL: iter_is_callable=" .. tostring(result.iter_is_callable) ..
          ", second_is_nil=" .. tostring(result.second_is_nil) ..
          ", third_is_nil=" .. tostring(result.third_is_nil) ..
          ", fourth_is_file=" .. tostring(result.fourth_is_file))
end
