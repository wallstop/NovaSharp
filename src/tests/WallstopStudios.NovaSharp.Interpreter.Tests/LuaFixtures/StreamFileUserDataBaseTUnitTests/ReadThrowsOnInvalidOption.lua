-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StreamFileUserDataBaseTUnitTests.cs:1031
-- @test: StreamFileUserDataBaseTUnitTests.ReadThrowsOnInvalidOption
-- Compatibility notes: Uses injected variable: file
local ok, err = pcall(function()
                    return file:read('*z')
                end)
                return ok, err
