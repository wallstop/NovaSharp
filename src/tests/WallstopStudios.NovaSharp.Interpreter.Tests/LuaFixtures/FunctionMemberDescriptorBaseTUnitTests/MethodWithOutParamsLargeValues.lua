-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Interop/Descriptors/FunctionMemberDescriptorBaseTUnitTests.cs:427
-- @test: FunctionMemberDescriptorBaseTUnitTests.MethodWithOutParamsLargeValues
-- @compat-notes: Lua 5.3+: bitwise OR; Uses injected variable: obj
local nil_val, out1, out2 = obj.VoidWithOut(2147483647, -2147483648)
                return tostring(nil_val) .. '|' .. out1 .. '|' .. out2
