-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:2240
-- @test: StringModuleTUnitTests.FormatSWithIntegerSubtypeLua53Plus
-- @compat-notes: Test targets Lua 5.3+; Lua 5.3+: math.maxinteger (5.3+); Uses injected variable: s
return string.format('%s', math.maxinteger)
