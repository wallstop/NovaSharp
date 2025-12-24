-- @lua-versions: 5.1, 5.2
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:1215
-- @test: StringModuleTUnitTests.CharHandlesNaNAsZeroLua51And52
-- @compat-notes: Test targets Lua 5.1
return string.char(0/0)
