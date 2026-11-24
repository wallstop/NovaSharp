-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/TableModuleTUnitTests.cs:56
-- @test: TableModuleTUnitTests.RegisteredTableCallbacksUseArgumentViews
local values = {3, 1}
                table.insert(values, 2, 2)
                table.sort(values)
                assert(table.remove(values, 2) == 2)
                assert(table.concat(values, '-') == '1-3')

                local unpackRange = table.unpack or unpack
                local first, last = unpackRange(values)
                assert(first == 1 and last == 3)
                assert(select('#', unpackRange(values, 2, 1)) == 0)
                assert(select('#', unpackRange(values, 2, 2)) == 1)

                if table.maxn then
                    assert(table.maxn(values) == 2, 'maxn ignores the removed array entry')
                    values[3.5] = true
                    assert(table.maxn(values) == 3.5, 'maxn includes fractional keys')
                    values[3.5] = nil
                    assert(table.maxn(values) == 2, 'maxn ignores the removed fractional key')
                end
                if _VERSION ~= 'Lua 5.1' then
                    local function items() return 1, nil, 3 end
                    local packed = table.pack(items())
                    assert(packed.n == 3 and packed[2] == nil and packed[3] == 3)
                end
                if _VERSION ~= 'Lua 5.1' and _VERSION ~= 'Lua 5.2' then
                    assert(table.move(values, 1, 2, 2) == values)
                    assert(values[2] == 1 and values[3] == 3)
                end
