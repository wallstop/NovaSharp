-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Units\Interop\Descriptors\FunctionMemberDescriptorBaseTUnitTests.cs:82
-- @test: FunctionMemberDescriptorBaseTUnitTests.VarArgsWithUserDataArrayPassthrough
-- @compat-notes: Lua 5.3+: bitwise operators; Uses injected variable: obj
local arr = {1, 2, 3, 4, 5}
                return obj.SumVarArgs(1, 2, 3)
