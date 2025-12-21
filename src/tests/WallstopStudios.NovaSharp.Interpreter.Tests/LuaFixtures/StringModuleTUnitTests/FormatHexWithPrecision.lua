-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:1619
-- @test: StringModuleTUnitTests.FormatHexWithPrecision
-- @compat-notes: Test targets Lua 5.1
return string.format('%.4x', 255)
