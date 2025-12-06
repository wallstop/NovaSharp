-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Units/HardwiredDescriptorsTUnitTests.cs:92
-- @test: HardwiredDescriptorsTUnitTests.HardwiredMethodDescriptorUsesProvidedArguments
-- @compat-notes: Uses injected variable: obj
return obj:call(7, 'custom')
