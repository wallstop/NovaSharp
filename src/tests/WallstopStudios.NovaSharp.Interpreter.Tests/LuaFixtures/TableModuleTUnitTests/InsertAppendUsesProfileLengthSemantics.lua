-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/TableModuleTUnitTests.cs:1098
-- @test: TableModuleTUnitTests.InsertAppendUsesProfileLengthSemantics
-- Compatibility notes: Lua 5.3+: math.maxinteger (5.3+); Lua 5.3+: math.mininteger (5.3+)
local lua51 = _VERSION == 'Lua 5.1'
                local lua52 = _VERSION == 'Lua 5.2'
                local invalidLength = lua52 and 'object length is not a number'
                    or 'object length is not an integer'

                local function check(length, index, expectedError)
                    local calls = 0
                    local values = {}
                    setmetatable(values, {__len = function(first, second)
                        assert(first == values and second == values, 'length operands')
                        calls = calls + 1
                        return length
                    end})

                    local ok, result = pcall(function()
                        return select('#', table.insert(values, 'new'))
                    end)
                    if lua51 then
                        assert(ok and result == 0 and calls == 0 and rawget(values, 1) == 'new')
                    elseif expectedError then
                        assert(not ok and calls == 1 and next(values) == nil, 'invalid length')
                        assert(string.find(result, expectedError, 1, true), result)
                    else
                        assert(ok and result == 0 and calls == 1, 'append length')
                        assert(rawget(values, index) == 'new', 'append index')
                    end
                end

                check(-2, -1)
                check(-1, 0)
                check('2', 3)
                check(false, nil, invalidLength)
                check('2' .. string.char(0) .. 'tail', nil, invalidLength)
                check(2.75, 3, not lua52 and invalidLength)
                check(2147483647, lua52 and -2147483648 or 2147483648)
                check(4294967296, lua52 and 1 or 4294967297)
                if math.maxinteger then
                    check(math.maxinteger, math.mininteger)
                end
