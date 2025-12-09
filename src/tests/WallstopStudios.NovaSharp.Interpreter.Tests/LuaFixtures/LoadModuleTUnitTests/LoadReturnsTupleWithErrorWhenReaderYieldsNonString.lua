-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\LoadModuleTUnitTests.cs:56
-- @test: LoadModuleTUnitTests.LoadReturnsTupleWithErrorWhenReaderYieldsNonString
-- @compat-notes: Lua 5.3+: bitwise operators
local called = false
                local function badreader()
                    if called then
                        return nil
                    end
                    called = true
                    return {}
                end

                return load(badreader)
