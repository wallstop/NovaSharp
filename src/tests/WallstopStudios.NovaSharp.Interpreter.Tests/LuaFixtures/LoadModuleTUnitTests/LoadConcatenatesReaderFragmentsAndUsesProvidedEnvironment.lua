-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/LoadModuleTUnitTests.cs:137
-- @test: LoadModuleTUnitTests.LoadConcatenatesReaderFragmentsAndUsesProvidedEnvironment
-- @compat-notes: Test targets Lua 5.1
local fragments = { 'return ', 'value', nil }
                local index = 0
                local reader = function()
                    index = index + 1
                    return fragments[index]
                end
                local env = { value = 123 }
                local chunk, err = load(reader, 'chunk-fragments', 't', env)
                assert(chunk ~= nil and err == nil)
                return chunk()
