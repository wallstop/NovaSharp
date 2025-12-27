-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:2219
-- @test: StringModuleTUnitTests.FormatSWithNegativeZero
-- @compat-notes: Uses injected variable: s
return string.format('%s', -0.0)
