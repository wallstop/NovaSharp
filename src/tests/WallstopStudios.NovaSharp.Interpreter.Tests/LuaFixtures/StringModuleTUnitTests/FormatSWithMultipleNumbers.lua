-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:2194
-- @test: StringModuleTUnitTests.FormatSWithMultipleNumbers
-- @compat-notes: Uses injected variable: s
return string.format('Values: %s, %s, %s', 1, 2.5, -3)
