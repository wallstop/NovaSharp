-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/LoadModuleVersionParityTUnitTests.cs:299
-- @test: LoadModuleVersionParityTUnitTests.LoadWithReaderConcatenatesFragmentsInAllVersions
local parts = { 'return ', '1 + ', '2' }
                local index = 0
                local function reader()
                    index = index + 1
                    return parts[index]
                end
                local f = load(reader)
                return f()
