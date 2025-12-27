-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:2457
-- @test: StringModuleTUnitTests.ByteOutOfBoundsIndicesAllVersions
-- @compat-notes: NovaSharp: unresolved C# interpolation placeholder
return string.byte('Lua', {indexExpression})
