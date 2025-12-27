-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/LoadModuleVersionParityTUnitTests.cs:237
-- @test: LoadModuleVersionParityTUnitTests.LoadstringSyntaxErrorReturnsNilAndMessageInLua51
-- @compat-notes: loadstring returns nil + error message on syntax error

-- Test: loadstring returns nil and error message on syntax error in Lua 5.1
-- Reference: Lua 5.1 Reference Manual §5.1 - loadstring

local fn, err = loadstring("invalid lua syntax @@#$")
assert(fn == nil, "loadstring should return nil on syntax error")
assert(type(err) == "string", "loadstring should return error message as string")
assert(#err > 0, "error message should not be empty")

print("PASS: loadstring returns nil + error on syntax error in Lua 5.1")
return "syntax_error_handled"
