-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Spec/LuaBasicMultiVersionSpecTUnitTests.cs:22
-- @test: LuaBasicMultiVersionSpecTUnitTests.ToNumberParsesIntegersAcrossSupportedBases
return tonumber('1010', 2),
                       tonumber('-77', 8),
                       tonumber('+1e', 16),
                       tonumber('Z', 36)
