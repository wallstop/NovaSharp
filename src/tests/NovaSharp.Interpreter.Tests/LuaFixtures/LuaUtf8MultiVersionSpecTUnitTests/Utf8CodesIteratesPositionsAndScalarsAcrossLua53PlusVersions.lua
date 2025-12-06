-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Spec/LuaUtf8MultiVersionSpecTUnitTests.cs:101
-- @test: LuaUtf8MultiVersionSpecTUnitTests.Utf8CodesIteratesPositionsAndScalarsAcrossLua53PlusVersions
-- @compat-notes: Test targets Lua 5.3+; Lua 5.3+: bitwise operators; Lua 5.3+: utf8 library
local parts = {}
                    for pos, cp in utf8.codes(word) do
                        parts[#parts + 1] = string.format('%d:%X', pos, cp)
                    end
                    return table.concat(parts, ',')
