-- @lua-versions: 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/Lua55SpecTUnitTests.cs:79
-- @test: Lua55SpecTUnitTests.StringCharProducesCorrectOutput
-- @compat-notes: Test targets Lua 5.5+
return string.char(97, 98, 99)
