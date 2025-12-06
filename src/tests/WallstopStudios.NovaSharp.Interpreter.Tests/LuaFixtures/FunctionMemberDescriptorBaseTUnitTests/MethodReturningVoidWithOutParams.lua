-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Units/FunctionMemberDescriptorBaseTUnitTests.cs:140
-- @test: FunctionMemberDescriptorBaseTUnitTests.MethodReturningVoidWithOutParams
-- @compat-notes: Lua 5.3+: bitwise operators; Lua 5.3+: bitwise OR; Uses injected variable: obj
local nil_val, out1, out2 = obj.VoidWithOut(5, 10)
                return tostring(nil_val) .. '|' .. out1 .. '|' .. out2
