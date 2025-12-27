-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:2254
-- @test: StringModuleTUnitTests.FormatSWithIntegerSubtypeLua53Plus
-- @compat-notes: Test targets Lua 5.3+; Lua 5.3+: math.mininteger (5.3+); Uses injected variable: s
return string.format('%s', math.mininteger)
