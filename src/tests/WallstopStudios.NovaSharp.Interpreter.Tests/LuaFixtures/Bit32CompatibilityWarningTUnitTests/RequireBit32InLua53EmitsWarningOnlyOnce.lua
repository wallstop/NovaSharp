-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Units\Compatibility\Bit32CompatibilityWarningTUnitTests.cs:29
-- @test: Bit32CompatibilityWarningTUnitTests.RequireBit32InLua53EmitsWarningOnlyOnce
-- @compat-notes: NovaSharp extension - bit32 is only available natively in Lua 5.2; in Lua 5.3+ NovaSharp provides it as a compatibility shim that emits a warning
local ok1 = pcall(function() require('bit32') end)
                local ok2 = pcall(function() require('bit32') end)
                assert(ok1 == false and ok2 == false)
