-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:2852
-- @test: DebugModuleTUnitTests.GetLocalDataDrivenEdgeCases
-- @compat-notes: NovaSharp: unresolved C# interpolation placeholder
local function sample(arg1, arg2, arg3)
                    local loc1 = 'local1'
                    local loc2 = 'local2'
                    local name, value = debug.getlocal(1, {index})
                    return name, value
                end
                return sample('a', 'b', 'c')
