-- @lua-versions: 5.2, 5.3
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs
-- @test: DebugModuleTUnitTests.UpvalueIdThrowsForInvalidIndexPreLua54

-- Test: debug.upvalueid throws error for invalid indices in Lua 5.2/5.3
-- Reference: Lua 5.2/5.3 manual - debug.upvalueid
-- @compat-notes: Lua 5.2/5.3 throw "invalid upvalue index" for invalid indices; Lua 5.4+ return nil

local function f() end
-- Index 999 is far beyond available upvalues
return debug.upvalueid(f, 999)
