-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/DataTypes/VmCorrectnessRegressionTUnitTests.cs:306
-- @test: MutableHashUserDataKey.ClosureUpValueSharingStillWorks
local count = 0
                local function inc() count = count + 1 end
                local function get() return count end
                
                inc()
                inc()
                return get()
