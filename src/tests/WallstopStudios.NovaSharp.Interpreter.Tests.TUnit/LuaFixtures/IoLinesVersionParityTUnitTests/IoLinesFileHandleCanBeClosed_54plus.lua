-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/IoLinesVersionParityTUnitTests.cs
-- @test: IoLinesVersionParityTUnitTests.IoLinesFileHandleCanBeClosedManuallyInLua54Plus
-- @compat-notes: Lua 5.4+ allows manual closing of the file handle returned by io.lines

-- Test: io.lines file handle can be closed manually in Lua 5.4+
-- Reference: Lua 5.4 Reference Manual §6.8

-- Create a temp file
local tmpname = os.tmpname()
local f = io.open(tmpname, "w")
f:write("line1\nline2\n")
f:close()

local iter, a, b, fh = io.lines(tmpname)
local type_before_close = io.type(fh)
fh:close()
local type_after_close = io.type(fh)

os.remove(tmpname)

if type_before_close == "file" and type_after_close == "closed file" then
    print("PASS")
else
    print("FAIL: type_before_close=" .. tostring(type_before_close) ..
          ", type_after_close=" .. tostring(type_after_close))
end
