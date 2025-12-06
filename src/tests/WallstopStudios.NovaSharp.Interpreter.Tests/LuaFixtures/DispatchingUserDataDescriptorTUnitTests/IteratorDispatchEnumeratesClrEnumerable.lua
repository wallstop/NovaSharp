-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Descriptors/DispatchingUserDataDescriptorTUnitTests.cs:228
-- @test: DispatchingUserDataDescriptorTUnitTests.IteratorDispatchEnumeratesClrEnumerable
-- @compat-notes: Lua 5.3+: bitwise operators
local total = 0
                for value in hostAdd do
                    total = total + value
                end
                return total
