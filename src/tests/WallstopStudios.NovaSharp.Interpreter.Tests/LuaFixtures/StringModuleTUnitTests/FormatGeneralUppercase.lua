-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:1862
-- @test: StringModuleTUnitTests.FormatGeneralUppercase
-- @compat-notes: Test targets Lua 5.1
return string.format('%G', 0.0001234)
