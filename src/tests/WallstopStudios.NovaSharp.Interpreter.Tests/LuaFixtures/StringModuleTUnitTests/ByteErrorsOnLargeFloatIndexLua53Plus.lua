-- @lua-versions: 5.3+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\StringModuleTUnitTests.cs:407
-- @test: StringModuleTUnitTests.ByteErrorsOnLargeFloatIndexLua53Plus
-- Test targets Lua 5.3+
return string.byte('a', 1e308)
