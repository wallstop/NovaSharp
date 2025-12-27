-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:2530
-- @test: DebugModuleTUnitTests.GetInfoDataDrivenWhatFlags
-- @compat-notes: NovaSharp: unresolved C# interpolation placeholder
local function sample(a, b)
                    local c = a + b
                    return c
                end
                local info = debug.getinfo(sample, '{whatFlag}')
                return info.{expectedField} ~= nil
