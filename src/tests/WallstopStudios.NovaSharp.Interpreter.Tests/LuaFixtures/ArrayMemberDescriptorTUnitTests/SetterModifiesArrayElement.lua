-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Descriptors/ArrayMemberDescriptorTUnitTests.cs:97
-- @test: ArrayMemberDescriptorTUnitTests.SetterModifiesArrayElement
-- @compat-notes: Uses injected variable: arr
arr[1] = 99
