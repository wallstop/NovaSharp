-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/LoadModuleTUnitTests.cs:86
-- @test: LoadModuleTUnitTests.LoadPropagatesDecoratedMessageWhenReaderThrowsSyntaxError
local function throwing_reader()
                    return throw_reader_helper()
                end
                return load(throwing_reader)
