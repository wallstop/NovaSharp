-- @lua-versions: 5.1+
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Descriptors/DispatchingUserDataDescriptorTUnitTests.cs:267
-- @test: DispatchingUserDataDescriptorTUnitTests.IteratorDispatchEnumeratesClrEnumerable
local total = 0
                for value in hostAdd do
                    total = total + value
                end
                return total
