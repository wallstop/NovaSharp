-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/SimpleTUnitTests.cs:179
-- @test: SimpleTUnitTests.CSharpStaticFunctionCall2
-- @compat-notes: Uses injected variable: callback
return callback 'hello';
