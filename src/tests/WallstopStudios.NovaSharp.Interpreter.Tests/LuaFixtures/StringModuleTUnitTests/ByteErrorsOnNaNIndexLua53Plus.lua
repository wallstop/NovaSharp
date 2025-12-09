-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\StringModuleTUnitTests.cs:278
-- @test: StringModuleTUnitTests.ByteErrorsOnNaNIndexLua53Plus
-- @compat-notes: Test targets Lua 5.3+
return string.byte('Lua', 0/0)
