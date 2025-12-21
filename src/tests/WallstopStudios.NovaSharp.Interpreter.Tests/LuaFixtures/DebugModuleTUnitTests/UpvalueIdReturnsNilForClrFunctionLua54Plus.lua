-- @lua-versions: 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:291
-- @test: DebugModuleTUnitTests.UpvalueIdReturnsNilForClrFunctionLua54Plus
-- @compat-notes: Test targets Lua 5.1; Lua 5.2+: debug.upvalueid (5.2+)
return debug.upvalueid(print, 1)
