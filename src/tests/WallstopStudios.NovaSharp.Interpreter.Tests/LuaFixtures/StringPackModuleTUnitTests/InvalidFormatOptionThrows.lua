-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/StringPackModuleTUnitTests.cs:352
-- @test: StringPackModuleTUnitTests.InvalidFormatOptionThrows
-- @compat-notes: Test targets Lua 5.3+; Lua 5.3+: string.pack (5.3+)
string.pack('Q', 42)
