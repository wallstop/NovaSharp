-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Interop/Descriptors/FunctionMemberDescriptorBaseTUnitTests.cs:503
-- @test: FunctionMemberDescriptorBaseTUnitTests.MethodWithSingleOutParamFailedParse
-- @compat-notes: Lua 5.3+: bitwise OR; Uses injected variable: obj
local ret, parsed = obj.TryParseInt('not_a_number')
                return tostring(ret) .. '|' .. parsed
