-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\DebugModuleTapParityTUnitTests.cs:62
-- @test: DebugModuleTapParityTUnitTests.GetInfoReturnsFunctionMetadata
local function sample()
                    return 1
                end
                return debug.getinfo(sample)
