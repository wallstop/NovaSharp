-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:196
-- @test: StringModuleTUnitTests.ByteTruncatesFloatIndices
return string.byte('Lua', 1.5)
