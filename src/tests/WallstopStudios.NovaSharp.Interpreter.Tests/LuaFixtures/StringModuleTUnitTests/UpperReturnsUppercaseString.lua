-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:345
-- @test: StringModuleTUnitTests.UpperReturnsUppercaseString
-- @compat-notes: Test targets Lua 5.1
return string.upper('NovaSharp')
