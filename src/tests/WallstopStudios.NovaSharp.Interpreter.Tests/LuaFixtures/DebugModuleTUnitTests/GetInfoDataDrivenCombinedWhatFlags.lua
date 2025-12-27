-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:2710
-- @test: DebugModuleTUnitTests.GetInfoDataDrivenCombinedWhatFlags
-- @compat-notes: NovaSharp: unresolved C# interpolation placeholder; Test targets Lua 5.5+
local function sample(a, b)
                    local c = a + b
                    return c
                end
                local info = debug.getinfo(sample, '{whatFlags}')
                return info.{field1} ~= nil, info.{field2} ~= nil
