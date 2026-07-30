-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Descriptors/ArrayMemberDescriptorTUnitTests.cs:100
-- @test: ArrayMemberDescriptorTUnitTests.SetterModifiesArrayElement
-- Uses injected variable: arr
arr[1] = 99
