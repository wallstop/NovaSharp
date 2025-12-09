-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Units\Interop\Descriptors\FunctionMemberDescriptorBaseTUnitTests.cs:103
-- @test: FunctionMemberDescriptorBaseTUnitTests.VarArgsWithUserDataArrayPassthroughSingleArg
-- @compat-notes: Uses injected variable: obj
return obj.SumVarArgs(arr)
