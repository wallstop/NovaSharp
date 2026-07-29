-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ScriptExecution/ScriptCompilationCacheTUnitTests.cs:261
-- @test: ScriptCompilationCacheTUnitTests.CachedLoadFunctionSetFenvKeepsIndependentEnvironmentSlots
-- Compatibility notes: Test targets Lua 5.1
local firstEnv = { value = 11 }
                local secondEnv = { value = 22 }
                setfenv(first, firstEnv)
                setfenv(second, secondEnv)
                return first(), second(), getfenv(first) ~= getfenv(second)
