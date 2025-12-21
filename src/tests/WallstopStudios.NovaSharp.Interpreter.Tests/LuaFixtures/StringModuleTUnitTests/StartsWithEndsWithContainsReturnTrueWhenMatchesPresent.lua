-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:963
-- @test: StringModuleTUnitTests.StartsWithEndsWithContainsReturnTrueWhenMatchesPresent
-- @compat-notes: NovaSharp: NovaSharp string extension; NovaSharp: NovaSharp string extension; NovaSharp: NovaSharp string extension; Test targets Lua 5.1
return string.startswith('NovaSharp', 'Nova'),
                       string.endswith('NovaSharp', 'Sharp'),
                       string.contains('NovaSharp', 'Shar')
