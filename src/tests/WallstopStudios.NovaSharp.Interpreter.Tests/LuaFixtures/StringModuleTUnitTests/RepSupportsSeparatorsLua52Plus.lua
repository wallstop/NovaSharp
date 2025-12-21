-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:718
-- @test: StringModuleTUnitTests.RepSupportsSeparatorsLua52Plus
-- @compat-notes: Test targets Lua 5.1
return string.rep('ab', 3, '-')
