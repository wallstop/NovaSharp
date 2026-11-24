-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/TableModuleTUnitTests.cs:1014
-- @test: TableModuleTUnitTests.InsertUsesArgumentCountAndReturnsNoValues
local function none() end
                local function pair() return 2, nil end
                local function extra() return 2, 4, 6 end

                local appended = {3, 5}
                assert(select('#', table.insert(appended, 7, none())) == 0, 'append result count')
                assert(appended[1] == 3 and appended[2] == 5 and appended[3] == 7)
                assert(select('#', table.insert(appended, nil)) == 0, 'append nil result count')
                assert(appended[3] == 7 and appended[4] == nil)

                local inserted = {3, 5}
                assert(select('#', table.insert(inserted, 2, 4)) == 0, 'insert result count')
                assert(inserted[1] == 3 and inserted[2] == 4 and inserted[3] == 5)
                assert(select('#', table.insert(inserted, 2, nil)) == 0, 'explicit nil result count')
                assert(inserted[1] == 3 and inserted[2] == nil and inserted[3] == 4 and inserted[4] == 5)

                local expanded = {3, 5}
                assert(select('#', table.insert(expanded, pair())) == 0, 'expanded result count')
                assert(expanded[1] == 3 and expanded[2] == nil and expanded[3] == 5 and expanded[4] == nil)

                local rejected = {3, 5}
                local ok, message = pcall(function() table.insert(rejected, none()) end)
                assert(not ok and string.find(message, "wrong number of arguments", 1, true), 'too few arguments')
                ok, message = pcall(function() table.insert(rejected, extra()) end)
                assert(not ok and string.find(message, "wrong number of arguments", 1, true), 'too many arguments')
                assert(rejected[1] == 3 and rejected[2] == 5 and rejected[3] == nil)

                local lengthCalls = 0
                local measured = setmetatable({}, {__len = function()
                    lengthCalls = lengthCalls + 1
                    return 0
                end})
                assert(not pcall(table.insert, measured))
                assert(lengthCalls == (_VERSION == 'Lua 5.1' and 0 or 1), 'length before arity')

                local binaryPosition = {3, 5}
                ok = pcall(table.insert, binaryPosition, '2' .. string.char(0) .. 'tail', 4)
                if _VERSION == 'Lua 5.1' then
                    assert(ok and binaryPosition[2] == 4 and binaryPosition[3] == 5)
                else
                    assert(not ok and binaryPosition[2] == 5 and binaryPosition[3] == nil)
                end

                local unicodePosition = {3, 5}
                ok = pcall(table.insert, unicodePosition, ' 2 ', 4)
                assert(not ok and unicodePosition[2] == 5 and unicodePosition[3] == nil)

                local positioned = {3, 5}
                table.insert(positioned, '2', 4)
                assert(positioned[1] == 3 and positioned[2] == 4 and positioned[3] == 5)
                ok, message = pcall(function() table.insert(positioned, 0, 2) end)
                if _VERSION == 'Lua 5.1' then
                    assert(ok and positioned[0] == 2 and positioned[1] == nil and positioned[2] == 3 and positioned[3] == 4 and positioned[4] == 5)
                    table.insert(positioned, 6, 6)
                    assert(positioned[6] == 6)
                    local fractional = {3, 5}
                    table.insert(fractional, -0.5, 2)
                    assert(fractional[-1] == nil and fractional[0] == 2 and fractional[2] == 3)

                    local sparse = {3, 5, [-101] = 'unchanged', [-99] = 'a', [-98] = 'b',
                        [-2] = 'c', [0] = 'd', [-50.5] = 'fractional'}
                    table.insert(sparse, -100, 'new')
                    assert(sparse[-101] == 'unchanged' and sparse[-100] == 'new')
                    assert(sparse[-99] == nil and sparse[-98] == 'a' and sparse[-97] == 'b')
                    assert(sparse[-2] == nil and sparse[-1] == 'c' and sparse[0] == nil)
                    assert(sparse[1] == 'd' and sparse[2] == 3 and sparse[3] == 5)
                    assert(sparse[-50.5] == 'fractional')
                else
                    assert(not ok and string.find(message, "position out of bounds", 1, true))
                    ok, message = pcall(function() table.insert(positioned, 6, 6) end)
                    assert(not ok and string.find(message, "position out of bounds", 1, true))
                    assert(positioned[1] == 3 and positioned[2] == 4 and positioned[3] == 5 and positioned[6] == nil)
                end
