-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:2363
-- @test: StringModuleTUnitTests.FormatHexWithNegativeInteger
-- @compat-notes: Test targets Lua 5.1
return string.format('%x', -1)
