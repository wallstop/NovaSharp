-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/TableModuleTUnitTests.cs:201
-- @test: TableModuleTUnitTests.InsertUsesLenMetamethodWhenPresent
-- @compat-notes: Test targets Lua 5.3+; Lua 5.3+: bitwise operators
local values = setmetatable({ [1] = 'seed' }, {
                    __len = function()
                        return 4
                    end
                })

                table.insert(values, 'sentinel')
                return values[5]
