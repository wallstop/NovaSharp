-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\StringModuleTUnitTests.cs:2591
-- @test: StringModuleTUnitTests.FindReturnsNilWhenNotFound
-- NovaSharp: unresolved C# interpolation placeholder
return string.find('{haystack}', '{needle}')
