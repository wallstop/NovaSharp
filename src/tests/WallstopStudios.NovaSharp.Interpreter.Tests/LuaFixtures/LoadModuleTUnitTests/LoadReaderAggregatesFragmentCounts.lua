-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/LoadModuleTUnitTests.cs:231
-- @test: LoadModuleTUnitTests.LoadReaderAggregatesFragmentCounts
-- Compatibility notes: NovaSharp: unresolved C# interpolation placeholder; Test targets Lua 5.1
local fragment_count = {fragmentCount}
                local emitted = 0
                local reader = function()
                    emitted = emitted + 1
                    if emitted == 1 then
                        return 'local total = 0\n'
                    end
                    if emitted <= fragment_count + 1 then
                        return 'total = total + 1\n'
                    end
                    if emitted == fragment_count + 2 then
                        return 'return total'
                    end
                    return nil
                end
                local chunk, err = load(reader, 'chunk-many-fragments')
                if chunk == nil then
                    error(err)
                end
                return chunk()
