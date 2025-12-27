-- @lua-versions: none
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/StringPackModuleTUnitTests.cs:308
-- @test: StringPackModuleTUnitTests.PackSizeUnavailableInLua51And52
-- @compat-notes: Test targets Lua 5.1; Lua 5.3+: string.packsize (5.3+)
string.packsize('i4')
