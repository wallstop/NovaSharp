-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Descriptors/ExtensionMethodsRegistryTUnitTests.cs:121
-- @test: ExtensionMethodsRegistryTUnitTests.ExtensionMethodCanBeCalledFromLua
local obj = TestClass.__new()
                return obj.TestExtensionMethod()
