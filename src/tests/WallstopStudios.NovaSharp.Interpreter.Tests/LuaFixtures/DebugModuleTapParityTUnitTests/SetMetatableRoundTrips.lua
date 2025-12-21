-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTapParityTUnitTests.cs:137
-- @test: DebugModuleTapParityTUnitTests.SetMetatableRoundTrips
local target = {}
                local mt = { flag = true }
                debug.setmetatable(target, mt)
                return debug.getmetatable(target) == mt
