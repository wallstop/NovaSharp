-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:2276
-- @test: StringModuleTUnitTests.FormatSMixedWithOtherSpecifiers
-- @compat-notes: Uses injected variable: s
return string.format('Name: %s, Value: %d, Ratio: %s', 'test', 42, 3.14)
