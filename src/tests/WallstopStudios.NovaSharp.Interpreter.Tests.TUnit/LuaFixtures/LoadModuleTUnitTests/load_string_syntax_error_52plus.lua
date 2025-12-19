-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/LoadModuleTUnitTests.cs
-- @test: LoadModuleTUnitTests.LoadReturnsTupleWithSyntaxErrorWhenStringIsInvalidLua52Plus

-- Test: load() with string containing syntax error returns (nil, error_message) in Lua 5.2+
-- Reference: Lua 5.2+ manual - load
-- @compat-notes: Lua 5.1 load() only accepts functions, not strings (use loadstring for strings)

local f, err = load('function(')
print("first return is nil:", f == nil)
print("second return is string:", type(err) == "string")
print("error message:", err)
return f == nil and type(err) == "string"
