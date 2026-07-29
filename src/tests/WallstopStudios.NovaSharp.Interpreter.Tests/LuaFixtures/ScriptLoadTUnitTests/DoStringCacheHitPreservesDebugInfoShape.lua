-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ScriptLoadTUnitTests.cs:2264
-- @test: ScriptLoadTUnitTests.DoStringCacheHitPreservesDebugInfoShape
-- Compatibility notes: Test targets Lua 5.1
local info = debug.getinfo(1, "fS")
                local funcInfo = debug.getinfo(info.func, "S")
                return type(info.func) .. ":" .. info.what .. ":" .. funcInfo.what .. ":" .. info.short_src .. ":" .. funcInfo.short_src
