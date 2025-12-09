-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:352
-- @test: StringModuleTUnitTests.ByteAcceptsLargeIntegerIndexLua53Plus
-- @compat-notes: Test targets Lua 5.3+
return string.byte('a', 9007199254740993)
