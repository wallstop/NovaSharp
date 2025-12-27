-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/IoLinesVersionParityTUnitTests.cs
-- @test: IoLinesVersionParityTUnitTests.IoLinesIteratesOverAllLines
-- @compat-notes: io.lines iteration works identically in all versions

-- Test: io.lines iterates over all lines
-- Reference: Lua Reference Manual §6.8

-- Create a temp file
local tmpname = os.tmpname()
local f = io.open(tmpname, "w")
f:write("first\nsecond\nthird\n")
f:close()

local lines = {}
for line in io.lines(tmpname) do
    lines[#lines + 1] = line
end

os.remove(tmpname)

if #lines == 3 and lines[1] == "first" and lines[2] == "second" and lines[3] == "third" then
    print("PASS")
else
    print("FAIL: got " .. #lines .. " lines: " .. 
          (lines[1] or "nil") .. ", " .. 
          (lines[2] or "nil") .. ", " .. 
          (lines[3] or "nil"))
end
