-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:1443
-- @test: StringModuleTUnitTests.FormatOctalWithLeftAlignAndAlternate
-- @compat-notes: Test targets Lua 5.1
return string.format('%-#8o', 8)
