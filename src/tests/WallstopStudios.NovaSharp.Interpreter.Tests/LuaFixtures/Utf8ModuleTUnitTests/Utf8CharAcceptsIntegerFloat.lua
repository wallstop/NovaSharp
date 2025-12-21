-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/Utf8ModuleTUnitTests.cs
-- @test: Utf8ModuleTUnitTests.Utf8CharAcceptsIntegerFloat
-- @compat-notes: Lua 5.3+ accepts floats that are exact integers (e.g., 65.0)
local result = utf8.char(65.0, 66.0)
assert(result == "AB", "Expected 'AB' but got '" .. tostring(result) .. "'")
return result
