-- @lua-versions: none
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/StringPackModuleTUnitTests.cs:295
-- @test: StringPackModuleTUnitTests.UnpackUnavailableInLua51And52
-- @compat-notes: Test targets Lua 5.1; Lua 5.3+: string.unpack (5.3+)
string.unpack('i4', 'test')
