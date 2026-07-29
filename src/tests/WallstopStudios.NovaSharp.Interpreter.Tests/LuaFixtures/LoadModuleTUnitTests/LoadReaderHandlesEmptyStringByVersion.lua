-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/LoadModuleTUnitTests.cs:271
-- @test: LoadModuleTUnitTests.LoadReaderHandlesEmptyStringByVersion
-- Compatibility notes: Test targets Lua 5.1
local emitted = 0
                local reader = function()
                    emitted = emitted + 1
                    if emitted == 1 then
                        return ''
                    end
                    if emitted == 2 then
                        return 'return 42'
                    end
                    return nil
                end
                local chunk, err = load(reader, 'chunk-empty-fragment')
                if chunk == nil then
                    error(err)
                end
                local value = chunk()
                if value == nil then
                    return nil
                end
                return value
