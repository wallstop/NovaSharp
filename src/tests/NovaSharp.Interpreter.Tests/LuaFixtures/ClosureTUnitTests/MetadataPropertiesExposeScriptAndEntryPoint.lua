-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Units/ClosureTUnitTests.cs:34
-- @test: ClosureTUnitTests.MetadataPropertiesExposeScriptAndEntryPoint
return function() return 42 end
