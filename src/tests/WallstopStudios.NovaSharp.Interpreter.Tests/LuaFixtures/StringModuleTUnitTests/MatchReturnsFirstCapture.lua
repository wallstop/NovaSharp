-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:778
-- @test: StringModuleTUnitTests.MatchReturnsFirstCapture
-- @compat-notes: Test targets Lua 5.1
return string.match('Version: 1.2.3', '%d+%.%d+%.%d+')
