-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/OsSystemModuleTUnitTests.cs:370
-- @test: OsSystemModuleTUnitTests.TimeMissingFieldRaisesError
-- @compat-notes: Lua 5.3+: bitwise operators
local ok, err = pcall(function()
                    return os.time({ year = 2000 })
                end)
                return ok, err
