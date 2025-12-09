-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Descriptors/ArrayMemberDescriptorTUnitTests.cs:92
-- @test: ArrayMemberDescriptorTUnitTests.SetterModifiesArrayElement
-- @compat-notes: Lua 5.3+: bitwise operators; Uses injected variable: arr
arr[1] = 99
