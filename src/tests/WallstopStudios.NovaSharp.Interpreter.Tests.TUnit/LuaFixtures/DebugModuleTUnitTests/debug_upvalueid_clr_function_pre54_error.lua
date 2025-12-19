-- @lua-versions: 5.2, 5.3
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs
-- @test: DebugModuleTUnitTests.UpvalueIdThrowsForClrFunctionPreLua54

-- Test: debug.upvalueid throws error for C functions in Lua 5.2/5.3
-- Reference: Lua 5.2/5.3 manual - debug.upvalueid
-- @compat-notes: Lua 5.2/5.3 throw "invalid upvalue index" for C functions; Lua 5.4+ return nil

-- print is a C function with no accessible upvalues
-- In Lua 5.2/5.3, this throws an error
return debug.upvalueid(print, 1)
