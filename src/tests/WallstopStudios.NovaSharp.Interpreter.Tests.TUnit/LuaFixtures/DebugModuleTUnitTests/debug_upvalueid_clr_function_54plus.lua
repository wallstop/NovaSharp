-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs
-- @test: DebugModuleTUnitTests.UpvalueIdReturnsNilForClrFunctionLua54Plus

-- Test: debug.upvalueid returns nil for CLR/C functions in Lua 5.4+
-- Reference: Lua 5.4 manual - debug.upvalueid

-- print is a C function with no accessible upvalues
-- In Lua 5.4+, this returns nil instead of throwing an error
local result = debug.upvalueid(print, 1)
print(result == nil)
return result == nil
