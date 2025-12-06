-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTapParityTUnitTests.cs:62
-- @test: DebugModuleTapParityTUnitTests.Unknown
local function sample()
                    return 1
                end
                return debug.getinfo(sample)
