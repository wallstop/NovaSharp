-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Descriptors/ExtensionMethodsRegistryTUnitTests.cs:118
-- @test: ExtensionMethodsRegistryTUnitTests.Unknown
-- @compat-notes: Lua 5.3+: bitwise operators
local obj = TestClass.__new()
                return obj.TestExtensionMethod()
