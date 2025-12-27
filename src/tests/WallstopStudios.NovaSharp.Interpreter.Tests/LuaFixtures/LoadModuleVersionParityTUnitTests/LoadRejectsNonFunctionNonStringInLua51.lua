-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/LoadModuleVersionParityTUnitTests.cs:245
-- @test: LoadModuleVersionParityTUnitTests.LoadRejectsNonFunctionNonStringInLua51
-- @compat-notes: Test targets Lua 5.1
load(123)
