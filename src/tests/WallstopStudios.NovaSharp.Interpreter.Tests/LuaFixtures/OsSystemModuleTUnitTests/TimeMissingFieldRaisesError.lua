-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/OsSystemModuleTUnitTests.cs:426
-- @test: OsSystemModuleTUnitTests.TimeMissingFieldRaisesError
-- @compat-notes: Test class 'OsSystemModuleTUnitTests' uses NovaSharp-specific OsSystemModule functionality
local ok, err = pcall(function()
                    return os.time({ year = 2000 })
                end)
                return ok, err
