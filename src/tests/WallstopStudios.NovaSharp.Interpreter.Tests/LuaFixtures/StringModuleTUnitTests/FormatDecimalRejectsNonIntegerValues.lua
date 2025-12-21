-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:2249
-- @test: StringModuleTUnitTests.FormatDecimalRejectsNonIntegerValues
-- @compat-notes: NovaSharp: unresolved C# interpolation placeholder
return string.format('%d', {luaExpression})
