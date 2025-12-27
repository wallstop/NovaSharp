-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:2112
-- @test: StringModuleTUnitTests.FormatSWithNegativeInfinity
-- @compat-notes: Uses injected variable: s
return string.format('%s', -1/0)
