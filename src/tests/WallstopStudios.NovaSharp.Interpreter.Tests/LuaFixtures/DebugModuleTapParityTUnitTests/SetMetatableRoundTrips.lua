-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTapParityTUnitTests.cs:128
-- @test: DebugModuleTapParityTUnitTests.SetMetatableRoundTrips
-- @compat-notes: Lua 5.3+: bitwise operators
local target = {}
                local mt = { flag = true }
                debug.setmetatable(target, mt)
                return debug.getmetatable(target) == mt
