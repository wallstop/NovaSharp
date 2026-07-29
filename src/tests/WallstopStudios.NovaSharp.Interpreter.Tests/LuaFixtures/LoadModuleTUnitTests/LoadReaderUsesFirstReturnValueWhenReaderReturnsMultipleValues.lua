-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/LoadModuleTUnitTests.cs:317
-- @test: LoadModuleTUnitTests.LoadReaderUsesFirstReturnValueWhenReaderReturnsMultipleValues
-- Compatibility notes: Test targets Lua 5.1
local emitted = false
                local reader = function()
                    if emitted then
                        return nil, 'ignored'
                    end
                    emitted = true
                    return 'return 77', 'ignored'
                end
                local chunk, err = load(reader, 'chunk-multiple-reader-results')
                if chunk == nil then
                    error(err)
                end
                return chunk()
