-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\Utf8ModuleTUnitTests.cs:164
-- @test: Utf8ModuleTUnitTests.Utf8CodesIteratesPositionsAndScalars
-- @compat-notes: Test targets Lua 5.4+; Lua 5.3+: bitwise operators; Lua 5.3+: utf8 library
local parts = {}
                for pos, cp in utf8.codes(word) do
                    parts[#parts + 1] = string.format('%d:%X', pos, cp)
                end
                return table.concat(parts, ',')
