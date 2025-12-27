-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StreamFileUserDataBaseTUnitTests.cs:359
-- @test: StreamFileUserDataBaseTUnitTests.ClosedFileRejectsFurtherReads
-- @compat-notes: Uses injected variable: file
local f = file
                f:close()
                local ok, err = pcall(function()
                    return f:read()
                end)
                return ok, err
