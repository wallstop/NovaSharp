-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/Utf8ModuleTUnitTests.cs:1114
-- @test: Utf8ModuleTUnitTests.Utf8OffsetRejectsNonIntegerN
-- @compat-notes: Test targets Lua 5.3+; Lua 5.3+: utf8 library
return utf8.offset('abc', 1.5)
