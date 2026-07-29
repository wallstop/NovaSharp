-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/SimpleTUnitTests.cs:151
-- @test: SimpleTUnitTests.CSharpStaticFunctionCall3
-- Compatibility notes: Uses injected variable: callback
return callback();
