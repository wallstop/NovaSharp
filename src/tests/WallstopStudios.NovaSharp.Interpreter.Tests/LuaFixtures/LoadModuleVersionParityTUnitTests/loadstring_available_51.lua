-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/LoadModuleVersionParityTUnitTests.cs:40
-- @test: LoadModuleVersionParityTUnitTests.LoadstringIsAvailableInLua51
-- @compat-notes: loadstring is Lua 5.1 only - use load() in 5.2+

-- Test: loadstring should be a function in Lua 5.1
-- Reference: Lua 5.1 Reference Manual §5.1

assert(type(loadstring) == "function", "loadstring should be a function in Lua 5.1")
print("PASS: loadstring exists in Lua 5.1")
return type(loadstring)
