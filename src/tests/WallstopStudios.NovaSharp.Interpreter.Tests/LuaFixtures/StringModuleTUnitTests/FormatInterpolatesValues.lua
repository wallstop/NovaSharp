-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:924
-- @test: StringModuleTUnitTests.FormatInterpolatesValues
-- @compat-notes: Test targets Lua 5.1
return string.format('Value: %0.2f', 3.14159)
