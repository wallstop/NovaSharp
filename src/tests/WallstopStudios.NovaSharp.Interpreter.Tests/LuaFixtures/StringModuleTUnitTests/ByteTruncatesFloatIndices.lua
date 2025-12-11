-- @lua-versions: 5.1, 5.2
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs
-- @test: StringModuleTUnitTests.ByteTruncatesFloatIndicesLua51And52
-- Lua 5.1/5.2 silently truncate fractional indices via floor.
-- Lua 5.3+ require exact integer representation and throw an error.
return string.byte('Lua', 1.5)
