-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/LoadModuleTUnitTests.cs
-- @test: LoadModuleTUnitTests.LoadstringReturnsTupleWithSyntaxErrorWhenStringIsInvalid

-- Test: loadstring() with syntax error returns (nil, error_message) in Lua 5.1
-- Reference: Lua 5.1 manual - loadstring
-- @compat-notes: loadstring is deprecated in Lua 5.2+ (use load instead)

local f, err = loadstring('function(')
print("first return is nil:", f == nil)
print("second return is string:", type(err) == "string")
print("error message:", err)
return f == nil and type(err) == "string"
