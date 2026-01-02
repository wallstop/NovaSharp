-- @lua-versions: 5.3+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\StringModuleTUnitTests.cs:1563
-- @test: StringModuleTUnitTests.FormatOctalPreservesLargeIntegers
-- @compat-notes: Test targets Lua 5.3+; Lua 5.3+: math.maxinteger (5.3+)
return string.format('%o', math.maxinteger)
