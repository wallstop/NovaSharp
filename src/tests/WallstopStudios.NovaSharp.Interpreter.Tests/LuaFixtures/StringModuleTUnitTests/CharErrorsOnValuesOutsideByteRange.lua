-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:103
-- @test: StringModuleTUnitTests.CharErrorsOnValuesOutsideByteRange
-- @compat-notes: Test targets Lua 5.1
return string.char(-1, 256)
