-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:371
-- @test: StringModuleTUnitTests.ByteAcceptsLargeIntegerIndexLua53Plus
-- @compat-notes: Test targets Lua 5.1
return string.byte('a', 9007199254740993)
