-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StreamFileUserDataBaseTUnitTests.cs:728
-- @test: StreamFileUserDataBaseTUnitTests.ReadParsesNumbersWithLeadingWhitespace
-- @compat-notes: Uses injected variable: file
local f = file
                local number = f:read('*n')
                local rest = f:read('*a')
                return number, rest
