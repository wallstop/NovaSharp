-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/TableModuleTUnitTests.cs:38
-- @test: TableModuleTUnitTests.PackPreservesExpandedNilAndReportsCount
-- Compatibility notes: Test targets Lua 5.2+; Lua 5.2+: table.pack (5.2+)
local function values()
                    return 'a', nil, 'c'
                end

                local packed = table.pack('head', values())
                assert(packed.n == 4, 'packed count')
                assert(packed[1] == 'head', 'packed head')
                assert(packed[2] == 'a', 'packed expanded first')
                assert(packed[3] == nil, 'packed expanded nil')
                assert(packed[4] == 'c', 'packed expanded third')

                return packed.n, packed[1], packed[2], packed[3], packed[4]
