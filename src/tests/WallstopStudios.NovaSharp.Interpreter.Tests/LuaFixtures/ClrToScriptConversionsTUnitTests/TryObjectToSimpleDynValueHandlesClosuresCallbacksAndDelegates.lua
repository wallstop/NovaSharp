-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/VM/ClrToScriptConversionsTUnitTests.cs:70
-- @test: ClrToScriptConversionsTUnitTests.TryObjectToSimpleDynValueHandlesClosuresCallbacksAndDelegates
return function(a) return a end
