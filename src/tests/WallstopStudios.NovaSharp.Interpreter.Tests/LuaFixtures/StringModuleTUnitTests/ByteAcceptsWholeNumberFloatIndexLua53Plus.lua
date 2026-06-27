-- @lua-versions: 5.3+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\StringModuleTUnitTests.cs:423
-- @test: StringModuleTUnitTests.ByteAcceptsWholeNumberFloatIndexLua53Plus
-- Test targets Lua 5.3+
return string.byte('hello', 5.0)
