-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Sandbox/SandboxMemoryLimitTUnitTests.cs:610
-- @test: SandboxMemoryLimitTUnitTests.ScriptMemoryLimitCallbackCanAllowContinuation
-- @compat-notes: Lua 5.3+: bitwise operators
local tables = {}
                for i = 1, 1000 do
                    tables[i] = { a = i, b = i * 2, c = i * 3 }
                end
