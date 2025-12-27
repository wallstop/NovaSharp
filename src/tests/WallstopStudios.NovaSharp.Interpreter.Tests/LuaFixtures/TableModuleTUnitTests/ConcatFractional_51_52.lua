-- Tests that table.concat(t, sep, i, j) accepts fractional indices in Lua 5.1/5.2
-- These versions silently truncate via floor

-- @lua-versions: 5.1, 5.2
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/TableModuleTUnitTests.cs
-- @test: TableModuleTUnitTests.ConcatFractional_51_52
local t = {"a", "b", "c"}
local result = table.concat(t, ",", 1.5, 2.9)  -- Should be concat(t, ",", 1, 2)
assert(result == "a,b", "Expected 'a,b', got: " .. result)
print("PASS: table.concat with fractional indices accepted")
