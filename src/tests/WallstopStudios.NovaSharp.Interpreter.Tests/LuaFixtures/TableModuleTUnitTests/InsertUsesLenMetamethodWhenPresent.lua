-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/TableModuleTUnitTests.cs:231
-- @test: TableModuleTUnitTests.InsertUsesLenMetamethodWhenPresent
-- @compat-notes: Test targets Lua 5.1
local values = setmetatable({ [1] = 'seed' }, {
                    __len = function()
                        return 4
                    end
                })

                table.insert(values, 'sentinel')
                return values[5]
