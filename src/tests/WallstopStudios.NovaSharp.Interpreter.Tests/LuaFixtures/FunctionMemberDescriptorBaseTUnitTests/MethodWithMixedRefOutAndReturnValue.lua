-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Units\Interop\Descriptors\FunctionMemberDescriptorBaseTUnitTests.cs:466
-- @test: FunctionMemberDescriptorBaseTUnitTests.MethodWithMixedRefOutAndReturnValue
-- @compat-notes: Lua 5.3+: bitwise operators; Lua 5.3+: bitwise OR; Uses injected variable: obj
local ret, ref_out, pure_out = obj.ComplexRefOutMethod(10, 5)
                return ret .. '|' .. ref_out .. '|' .. pure_out
