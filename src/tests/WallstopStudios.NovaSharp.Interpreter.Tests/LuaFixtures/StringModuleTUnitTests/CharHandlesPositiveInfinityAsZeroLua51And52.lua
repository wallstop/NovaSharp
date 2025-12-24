-- @lua-versions: 5.1, 5.2
-- @novasharp-only: true
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:1229
-- @test: StringModuleTUnitTests.CharErrorsOnPositiveInfinityLua51And52
-- Lua 5.1/5.2: Positive infinity throws "invalid value" error
return string.char(1/0)
