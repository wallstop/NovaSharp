-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/CoreLib/StringFormatFiniteTUnitTests.cs:70
-- @test: StringFormatFiniteTUnitTests.FormatPadsIntegerPrecision
-- Compatibility notes: Test targets Lua 5.4+
return table.concat({
                    string.format('%.5d', 42),
                    string.format('%.5o', 42),
                    string.format('%.5x', 42),
                    string.format('%.5u', 42)
                }, ',')
