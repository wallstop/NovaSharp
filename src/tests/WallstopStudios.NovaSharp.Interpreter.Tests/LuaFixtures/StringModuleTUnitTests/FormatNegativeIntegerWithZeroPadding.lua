-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:1755
-- @test: StringModuleTUnitTests.FormatNegativeIntegerWithZeroPadding
-- @compat-notes: Test targets Lua 5.1
return string.format('%08d', -42)
