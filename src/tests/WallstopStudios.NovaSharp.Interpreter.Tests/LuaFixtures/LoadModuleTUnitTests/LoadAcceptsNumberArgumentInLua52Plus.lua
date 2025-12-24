-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/LoadModuleTUnitTests.cs
-- @test: LoadModuleTUnitTests.LoadAcceptsNumberArgumentInLua52Plus
-- @compat-notes: Lua 5.2+ load() accepts numbers and converts them to strings before parsing

-- Test: load() should accept a number and convert it to string
-- Reference: Lua 5.2+ converts number to string, then tries to parse as Lua code
-- load(123) -> parses "123" which is not valid Lua syntax, returns (nil, error)
local chunk, err = load(123)
assert(chunk == nil, "Expected nil for invalid Lua code")
assert(type(err) == "string", "Expected error message string")
print("PASS: load(123) returns (nil, error) as expected")
