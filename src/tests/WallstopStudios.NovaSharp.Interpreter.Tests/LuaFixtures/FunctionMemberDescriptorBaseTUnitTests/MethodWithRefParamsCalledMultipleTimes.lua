-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Interop/Descriptors/FunctionMemberDescriptorBaseTUnitTests.cs:369
-- @test: FunctionMemberDescriptorBaseTUnitTests.MethodWithRefParamsCalledMultipleTimes
-- @compat-notes: NovaSharp: unresolved C# interpolation placeholder
local upper, concat, lower = obj.ManipulateString('{input}', '{refValue}')
                    return upper .. '|' .. concat .. '|' .. lower
