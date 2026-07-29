-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:3121
-- @test: DebugModuleTUnitTests.GetInfoReportsParameterAndVarargMetadata
-- Compatibility notes: Test targets Lua 5.1
local function describe(value)
                    if value == nil then
                        return 'nil'
                    end
                    return tostring(value)
                end

                local function fixed(a, b)
                    return a + b
                end

                local function vararg(a, b, ...)
                    local active = debug.getinfo(1, 'u')
                    return active.nparams, active.isvararg
                end

                local fixedInfo = debug.getinfo(fixed, 'u')
                local varargInfo = debug.getinfo(vararg, 'u')
                local activeParams, activeVararg = vararg(1, 2, 3)

                return table.concat({
                    describe(fixedInfo.nparams),
                    describe(fixedInfo.isvararg),
                    describe(varargInfo.nparams),
                    describe(varargInfo.isvararg),
                    describe(activeParams),
                    describe(activeVararg),
                }, ':')
