-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/BasicModuleTUnitTests.cs:441
-- @test: BasicModuleTUnitTests.SelectErrorsOnNonIntegerIndexLua53Plus
-- @compat-notes: Test targets Lua 5.1
return select(1.5, 'a', 'b', 'c')
