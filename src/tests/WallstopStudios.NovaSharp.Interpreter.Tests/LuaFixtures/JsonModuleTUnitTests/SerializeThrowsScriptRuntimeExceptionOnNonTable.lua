-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/JsonModuleTUnitTests.cs:88
-- @test: JsonModuleTUnitTests.SerializeThrowsScriptRuntimeExceptionOnNonTable
-- @compat-notes: Test class 'JsonModuleTUnitTests' uses NovaSharp-specific JsonModule functionality
return require('json')
