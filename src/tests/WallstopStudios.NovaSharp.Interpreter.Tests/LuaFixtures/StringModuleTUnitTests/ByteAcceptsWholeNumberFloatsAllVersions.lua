-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:2483
-- @test: StringModuleTUnitTests.ByteAcceptsWholeNumberFloatsAllVersions
-- @compat-notes: NovaSharp: unresolved C# interpolation placeholder
return string.byte('Lua', {indexExpression})
