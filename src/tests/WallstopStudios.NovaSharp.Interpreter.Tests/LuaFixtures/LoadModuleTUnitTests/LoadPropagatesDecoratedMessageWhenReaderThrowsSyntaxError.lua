-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/LoadModuleTUnitTests.cs:86
-- @test: LoadModuleTUnitTests.LoadPropagatesDecoratedMessageWhenReaderThrowsSyntaxError
-- @compat-notes: Uses injected variable: throw_reader_helper
local function throwing_reader()
                    return throw_reader_helper()
                end
                return load(throwing_reader)
