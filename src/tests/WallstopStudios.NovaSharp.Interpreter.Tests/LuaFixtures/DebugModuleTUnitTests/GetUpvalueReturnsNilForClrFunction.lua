-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:236
-- @test: DebugModuleTUnitTests.GetUpvalueReturnsNilForClrFunction
-- @compat-notes: Test targets Lua 5.1
return debug.getupvalue(print, 1)
