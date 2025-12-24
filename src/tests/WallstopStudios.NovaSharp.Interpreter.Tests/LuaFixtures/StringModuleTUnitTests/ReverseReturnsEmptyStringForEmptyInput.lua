-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:794
-- @test: StringModuleTUnitTests.ReverseReturnsEmptyStringForEmptyInput
-- @compat-notes: Test targets Lua 5.1
return string.reverse('')
