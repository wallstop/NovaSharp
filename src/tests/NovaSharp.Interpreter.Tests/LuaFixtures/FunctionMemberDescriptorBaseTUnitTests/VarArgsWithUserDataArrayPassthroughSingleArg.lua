-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Units/FunctionMemberDescriptorBaseTUnitTests.cs:102
-- @test: FunctionMemberDescriptorBaseTUnitTests.VarArgsWithUserDataArrayPassthroughSingleArg
-- @compat-notes: Uses injected variable: obj
return obj.SumVarArgs(arr)
