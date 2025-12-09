-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleTUnitTests.cs:945
-- @test: IoModuleTUnitTests.OutputCanBeRedirectedToCustomFile
-- @compat-notes: NovaSharp: unresolved C# interpolation placeholder
local original = io.output()
                    local temp = assert(io.open('{escapedPath}', 'w'))
                    io.output(temp)
                    io.write('hello world')
                    io.flush()
                    io.output(original)
                    temp:close()
