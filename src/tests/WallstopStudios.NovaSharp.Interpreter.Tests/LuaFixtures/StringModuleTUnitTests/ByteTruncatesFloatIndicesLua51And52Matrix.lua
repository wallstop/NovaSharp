-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:2316
-- @test: StringModuleTUnitTests.ByteTruncatesFloatIndicesLua51And52Matrix
-- @compat-notes: NovaSharp: unresolved C# interpolation placeholder; Test targets Lua 5.1
return string.byte('Lua', {indexExpression})
