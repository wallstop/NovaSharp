-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:1746
-- @test: StringModuleTUnitTests.FormatFloatAcceptsAnyNumericValue
-- @compat-notes: NovaSharp: unresolved C# interpolation placeholder
return string.format('%f', {luaValue})
