-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Spec/LuaUtf8MultiVersionSpecTUnitTests.cs:43
-- @test: LuaUtf8MultiVersionSpecTUnitTests.Utf8LenCountsCharactersAndFlagsInvalidSequencesAcrossLua53PlusVersions
-- @compat-notes: Test targets Lua 5.2+; Lua 5.3+: utf8 library
return utf8.len(invalid)
