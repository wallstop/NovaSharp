-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/StringLibTUnitTests.cs:171
-- @test: StringLibTUnitTests.StringFormatPadsNumericArguments
d = 5; m = 11; y = 1990
                return string.format('%02d/%02d/%04d', d, m, y)
