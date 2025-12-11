-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: true
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\StringModuleTUnitTests.cs:121
-- @test: StringModuleTUnitTests.CharAcceptsBoundaryValues
-- @compat-notes: NovaSharp: unresolved C# interpolation placeholder
return string.char({value})
