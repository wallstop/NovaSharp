-- @lua-versions: 5.2+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:220
-- @test: DebugModuleTUnitTests.UpvalueIdReturnsNilForClrFunction
-- @compat-notes: Lua 5.2+: debug.upvalueid (5.2+)
return debug.upvalueid(print, 1)
