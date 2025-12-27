-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Interop/Descriptors/FunctionMemberDescriptorBaseTUnitTests.cs:338
-- @test: FunctionMemberDescriptorBaseTUnitTests.MethodWithOutParamsCalledMultipleTimes
-- @compat-notes: NovaSharp: unresolved C# interpolation placeholder
local nil_val, out1, out2 = obj.VoidWithOut({i}, {i * 2})
                    return tostring(nil_val) .. '|' .. out1 .. '|' .. out2
