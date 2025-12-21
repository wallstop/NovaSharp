-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Interop/Descriptors/FunctionMemberDescriptorBaseTUnitTests.cs:409
-- @test: FunctionMemberDescriptorBaseTUnitTests.MethodWithOutParamsNegativeValues
-- @compat-notes: Lua 5.3+: bitwise OR; Uses injected variable: obj
local nil_val, out1, out2 = obj.VoidWithOut(-100, -200)
                return tostring(nil_val) .. '|' .. out1 .. '|' .. out2
