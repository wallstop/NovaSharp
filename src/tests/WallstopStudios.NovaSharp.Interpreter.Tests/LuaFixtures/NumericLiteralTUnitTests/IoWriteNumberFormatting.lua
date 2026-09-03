-- @lua-versions: all
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/NumericLiteralTUnitTests.cs
-- @test: NumericLiteralTUnitTests.NumberToStringCoercionMatchesReference
-- io.write formats floats itself: plain %.14g in Lua 5.1-5.4 (no ".0" suffix)
-- and the tostring format in Lua 5.5.
io.write(42, " ", 2.0, " ", 0x10, " ", 2^53, " ", 1/3, "\n")
local f = io.open("io-write-formatting.tmp", "w")
f:write(2.0, "x", 42, " ", 2^53)
f:close()
for line in io.lines("io-write-formatting.tmp") do
    print(line)
end
os.remove("io-write-formatting.tmp")
