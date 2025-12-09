-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathIntegerFunctionsTUnitTests.cs:594
-- @test: MathIntegerFunctionsTUnitTests.TonumberIntegerBoundaryStrings
-- @compat-notes: NovaSharp: unresolved C# interpolation placeholder
return tonumber({expression})
