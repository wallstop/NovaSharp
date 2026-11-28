-- @lua-versions: 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/TableIteratorsModuleTUnitTests.cs:55
-- @test: TableIteratorsModuleTUnitTests.NextCallsUseStackOnlyArgumentsWithoutWrapperAllocations
-- Compatibility notes: Test targets Lua 5.5+
makeTable = function(n)
                    local t = {}
                    for i = 1, n do t[i] = i end
                    return t
                end
                prober = function(t, n)
                    local key = nil
                    for i = 1, n do
                        key = next(t, key)
                        if key == nil then key = nil end
                    end
                end
