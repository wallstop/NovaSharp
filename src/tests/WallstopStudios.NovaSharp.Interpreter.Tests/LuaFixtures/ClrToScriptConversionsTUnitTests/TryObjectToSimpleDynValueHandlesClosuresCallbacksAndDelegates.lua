-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Interop/Converters/ClrToScriptConversionsTUnitTests.cs:88
-- @test: ClrToScriptConversionsTUnitTests.TryObjectToSimpleDynValueHandlesClosuresCallbacksAndDelegates
return function(a) return a end
