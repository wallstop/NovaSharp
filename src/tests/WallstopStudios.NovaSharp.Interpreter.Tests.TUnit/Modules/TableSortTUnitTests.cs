namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.Modules
{
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.Modules;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.TUnit;

    public sealed class TableSortTUnitTests
    {
        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public void SortUsesBuiltInStringOrderAndReturnsNoValues(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            script.DoString(
                @"
                local calls = 0
                debug.setmetatable('', { __lt = function(a, b)
                    calls = calls + 1
                    return a > b
                end })

                local values = {'c', 'a', 'b'}
                local count = select('#', table.sort(values))
                assert(table.concat(values) == 'abc', 'default string order')
                assert(calls == 0, 'string pairs bypass __lt')
                assert(count == 0, 'default sort return count')

                local function descending(a, b) return a > b end
                assert(select('#', table.sort(values, descending)) == 0, 'explicit sort return count')
                assert(table.concat(values) == 'cba', 'explicit comparator')

                local mt = { __lt = function(a, b) return a.rank < b.rank end }
                local first = setmetatable({rank = 1}, mt)
                local second = setmetatable({rank = 2}, mt)
                local objects = {second, first}
                assert(select('#', table.sort(objects)) == 0, 'metamethod sort return count')
                assert(objects[1] == first and objects[2] == second, 'metamethod order')

                assert(select('#', table.sort({})) == 0, 'empty sort return count')
                local singleton = {'only'}
                assert(select('#', table.sort(singleton)) == 0, 'singleton sort return count')
                assert(singleton[1] == 'only', 'singleton value')

                local function fails() error('protected comparison') end
                local function succeeds() end
                local callbacks = {fails, succeeds}
                table.sort(callbacks, pcall)
                assert(callbacks[1] == succeeds, 'native comparator first result')

                local function compare(a, b) return a > b, true end
                local numbers = {1, 2}
                table.sort(numbers, compare)
                assert(numbers[1] == 2, 'Lua comparator first result')
                "
            );
        }

        [global::TUnit.Core.Test]
        [AllLuaVersions]
        public void SortValidatesComparatorAfterGettingLength(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            script.DoString(
                @"
                local reads = 0
                local function check(length, shouldSucceed)
                    local values = setmetatable({2, 1}, { __len = function()
                        reads = reads + 1
                        return length
                    end })
                    local ok, result = pcall(function()
                        return select('#', table.sort(values, false))
                    end)
                    assert(ok == shouldSucceed, 'comparator validation')
                    if ok then
                        assert(result == 0, 'short sort return count')
                    else
                        assert(result:find('function expected', 1, true), 'invalid comparator diagnostic')
                    end
                    assert(values[1] == 2 and values[2] == 1, 'values before sorting')
                end

                check(1, _VERSION ~= 'Lua 5.1' and _VERSION ~= 'Lua 5.2')
                check(2, false)
                assert(reads == (_VERSION == 'Lua 5.1' and 0 or 2), 'length reads')
                "
            );
        }

        [global::TUnit.Core.Test]
        [LuaVersionsFrom(LuaCompatibilityVersion.Lua53)]
        public void SortChecksFullLengthBeforeDecidingWhetherToSort(LuaCompatibilityVersion version)
        {
            Script script = new Script(version, CoreModulePresets.Complete);
            script.DoString(
                @"
                local function check(length, diagnostic)
                    local values = setmetatable({}, { __len = function() return length end })
                    local ok, result = pcall(function()
                        return select('#', table.sort(values, false))
                    end)
                    if diagnostic then
                        assert(not ok and result:find(diagnostic, 1, true), tostring(length))
                    else
                        assert(ok and result == 0, 'nontrivial length check')
                    end
                end
                check(0.5, 'object length is not an integer')
                check(1 / 0, 'object length is not an integer')
                check(2147483647, 'array too big')
                check(2147483648, 'array too big')
                check(-2147483649)
                "
            );
        }
    }
}
