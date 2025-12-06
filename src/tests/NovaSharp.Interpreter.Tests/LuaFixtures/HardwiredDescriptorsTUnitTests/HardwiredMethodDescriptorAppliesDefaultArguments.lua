-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: true
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Units/HardwiredDescriptorsTUnitTests.cs:71
-- @test: HardwiredDescriptorsTUnitTests.HardwiredMethodDescriptorAppliesDefaultArguments
-- @compat-notes: Uses injected variable: obj
return obj:call(5)
