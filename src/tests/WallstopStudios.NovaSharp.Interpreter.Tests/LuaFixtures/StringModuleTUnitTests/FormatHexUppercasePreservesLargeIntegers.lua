-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:1367
-- @test: StringModuleTUnitTests.FormatHexUppercasePreservesLargeIntegers
-- @compat-notes: Lua 5.3+: math.maxinteger (5.3+)
return string.format('%X', math.maxinteger)
