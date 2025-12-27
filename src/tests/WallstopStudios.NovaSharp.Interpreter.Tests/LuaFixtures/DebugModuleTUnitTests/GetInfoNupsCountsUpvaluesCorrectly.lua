-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:3049
-- @test: DebugModuleTUnitTests.GetInfoNupsCountsUpvaluesCorrectly
-- @compat-notes: Test targets Lua 5.1
local a, b, c = 1, 2, 3
                local function noExplicitUpvalues() return 42 end
                local function oneExplicitUpvalue() return a end
                local function threeExplicitUpvalues() return a + b + c end
                
                local info0 = debug.getinfo(noExplicitUpvalues, 'u')
                local info1 = debug.getinfo(oneExplicitUpvalue, 'u')
                local info3 = debug.getinfo(threeExplicitUpvalues, 'u')
                
                return info0.nups, info1.nups, info3.nups
