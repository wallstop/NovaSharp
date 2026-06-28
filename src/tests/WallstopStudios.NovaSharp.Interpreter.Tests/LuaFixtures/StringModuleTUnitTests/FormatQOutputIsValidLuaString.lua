-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\StringModuleTUnitTests.cs:2792
-- @test: StringModuleTUnitTests.FormatQOutputIsValidLuaString
local original = 'hello' .. string.char(9) .. 'world' .. string.char(10) .. 'end'
                local quoted = string.format('%q', original)
                -- Load the quoted string as Lua code (loadstring for 5.1, load for 5.2+)
                local loader = loadstring or load
                local func = loader('return ' .. quoted)
                if func then
                    local roundtrip = func()
                    return original == roundtrip
                else
                    return false
                end
