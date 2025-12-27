-- @lua-versions: 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/Lua55SpecTUnitTests.cs:115
-- @test: Lua55SpecTUnitTests.StringRepSupportsOptionalSeparator
-- @compat-notes: Test targets Lua 5.5+
return string.rep('ab', 3, '-')
