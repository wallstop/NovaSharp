-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:660
-- @test: StringModuleTUnitTests.FormatInterpolatesValues
-- @compat-notes: Test targets Lua 5.3+
return string.format('Value: %0.2f', 3.14159)
