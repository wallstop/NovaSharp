-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:223
-- @test: DebugModuleTUnitTests.UpvalueIdThrowsForClrFunction
-- @compat-notes: Lua 5.2+: debug.upvalueid (5.2+)
return debug.upvalueid(print, 1)
