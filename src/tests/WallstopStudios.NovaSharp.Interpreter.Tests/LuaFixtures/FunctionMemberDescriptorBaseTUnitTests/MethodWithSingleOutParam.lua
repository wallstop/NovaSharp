-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Interop/Descriptors/FunctionMemberDescriptorBaseTUnitTests.cs:487
-- @test: FunctionMemberDescriptorBaseTUnitTests.MethodWithSingleOutParam
-- Compatibility notes: Lua 5.3+: bitwise OR; Uses injected variable: obj
local ret, parsed = obj.TryParseInt('42')
                return tostring(ret) .. '|' .. parsed
