-- @lua-versions: none
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/StringPackModuleTUnitTests.cs:282
-- @test: StringPackModuleTUnitTests.PackUnavailableInLua51And52
-- @compat-notes: Test targets Lua 5.1; Lua 5.3+: string.pack (5.3+)
string.pack('i4', 42)
