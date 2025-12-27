-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StreamFileUserDataBaseTUnitTests.cs:445
-- @test: StreamFileUserDataBaseTUnitTests.ReadSupportsNumericAndAllModes
-- @compat-notes: Uses injected variable: file
local f = file
                local num = f:read('*n')
                f:seek('set', 5)
                local rest = f:read(2)
                local all = f:read('*a')
                return num, rest, all
