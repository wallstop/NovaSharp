-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\StringModuleTUnitTests.cs:2623
-- @test: StringModuleTUnitTests.FormatQEscaping
-- NovaSharp: unresolved C# interpolation placeholder
return string.format('%q', '{input}')
