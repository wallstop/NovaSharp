-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Sandbox/SandboxMemoryLimitTUnitTests.cs:610
-- @test: SandboxMemoryLimitTUnitTests.ScriptMemoryLimitCallbackCanAllowContinuation
-- @compat-notes: Test class 'SandboxMemoryLimitTUnitTests' uses NovaSharp-specific Sandbox functionality
local tables = {}
                for i = 1, 1000 do
                    tables[i] = { a = i, b = i * 2, c = i * 3 }
                end
