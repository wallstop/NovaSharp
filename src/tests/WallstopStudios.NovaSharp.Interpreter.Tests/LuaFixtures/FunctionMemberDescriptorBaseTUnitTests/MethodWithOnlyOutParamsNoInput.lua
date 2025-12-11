-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Units\Interop\Descriptors\FunctionMemberDescriptorBaseTUnitTests.cs:448
-- @test: FunctionMemberDescriptorBaseTUnitTests.MethodWithOnlyOutParamsNoInput
-- @compat-notes: Lua 5.3+: bitwise operators; Lua 5.3+: bitwise OR; Uses injected variable: obj
local nil_val, out1, out2, out3 = obj.GetMultipleOutValues()
                return tostring(nil_val) .. '|' .. out1 .. '|' .. out2 .. '|' .. tostring(out3)
