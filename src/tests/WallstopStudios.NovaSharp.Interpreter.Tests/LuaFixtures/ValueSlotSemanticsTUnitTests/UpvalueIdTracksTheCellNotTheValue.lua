-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/ValueSlotSemanticsTUnitTests.cs:192
-- @test: ValueSlotSemanticsTUnitTests.UpvalueIdTracksTheCellNotTheValue
-- Compatibility notes: Lua 5.2+: debug.upvalueid (5.2+)
local function mkNil() local v = nil return function() return v end end
                local function mkInt(n) local v = n return function() return v end end
                local function pair()
                    local v = 0
                    return function() return v end, function() v = v + 1 end
                end

                -- distinct variables that merely hold the same value must not collide
                local sharesNil = debug.upvalueid(mkNil(), 1) == debug.upvalueid(mkNil(), 1)
                local sharesInt = debug.upvalueid(mkInt(1), 1) == debug.upvalueid(mkInt(1), 1)

                -- two closures over one variable must share, and stay stable across assignment
                local read, bump = pair()
                local sharesLocal = debug.upvalueid(read, 1) == debug.upvalueid(bump, 1)
                local before = debug.upvalueid(read, 1)
                bump()
                bump()
                local stable = debug.upvalueid(read, 1) == before

                return sharesNil, sharesInt, sharesLocal, stable
