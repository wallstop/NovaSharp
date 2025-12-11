-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\BasicModuleTUnitTests.cs:618
-- @test: BasicModuleTUnitTests.SelectAcceptsIntegralFloatLua53Plus
-- @compat-notes: Test targets Lua 5.1
return select(2.0, 'a', 'b', 'c')
