-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Spec/LuaBasicMultiVersionSpecTUnitTests.cs:29
-- @test: LuaBasicMultiVersionSpecTUnitTests.ToNumberParsesIntegersAcrossSupportedBases
-- @compat-notes: Test targets Lua 5.1
return tonumber('1010', 2),
                       tonumber('-77', 8),
                       tonumber('+1e', 16),
                       tonumber('Z', 36)
