-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:2092
-- @test: StringModuleTUnitTests.FormatSWithPositiveInfinity
-- @compat-notes: Uses injected variable: s
return string.format('%s', 1/0)
