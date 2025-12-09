-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Compatibility/Bit32CompatibilityWarningTUnitTests.cs:29
-- @test: Bit32CompatibilityWarningTUnitTests.RequireBit32InLua53EmitsWarningOnlyOnce
-- @compat-notes: Test targets Lua 5.2+; Lua 5.3+: bitwise operators
local ok1 = pcall(function() require('bit32') end)
                local ok2 = pcall(function() require('bit32') end)
                assert(ok1 == false and ok2 == false)
