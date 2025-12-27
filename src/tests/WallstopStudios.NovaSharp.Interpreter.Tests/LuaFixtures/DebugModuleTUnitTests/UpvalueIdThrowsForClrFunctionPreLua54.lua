-- @lua-versions: none
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:308
-- @test: DebugModuleTUnitTests.UpvalueIdThrowsForClrFunctionPreLua54
-- @compat-notes: Test targets Lua 5.1; Lua 5.2+: debug.upvalueid (5.2+)
return debug.upvalueid(print, 1)
