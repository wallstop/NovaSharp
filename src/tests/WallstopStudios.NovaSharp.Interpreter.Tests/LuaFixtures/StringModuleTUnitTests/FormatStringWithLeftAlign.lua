-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:1940
-- @test: StringModuleTUnitTests.FormatStringWithLeftAlign
-- @compat-notes: Test targets Lua 5.1
return string.format('%-10s', 'Hello')
