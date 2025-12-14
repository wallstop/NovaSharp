-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/LoadModuleVersionParityTUnitTests.cs:50
-- @test: LoadModuleVersionParityTUnitTests.LoadstringIsNilInLua52Plus
-- @compat-notes: loadstring was removed in Lua 5.2 - use load() instead

-- Test: loadstring should be nil in Lua 5.2+
-- Reference: Lua 5.2+ Reference Manual - loadstring removed

assert(type(loadstring) == "nil", "loadstring should be nil in Lua 5.2+")
print("PASS: loadstring is nil in Lua 5.2+")
return type(loadstring)
