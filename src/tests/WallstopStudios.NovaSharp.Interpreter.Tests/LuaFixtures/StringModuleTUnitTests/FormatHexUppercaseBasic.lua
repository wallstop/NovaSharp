-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:1535
-- @test: StringModuleTUnitTests.FormatHexUppercaseBasic
-- @compat-notes: Test targets Lua 5.1
return string.format('%X', 255)
