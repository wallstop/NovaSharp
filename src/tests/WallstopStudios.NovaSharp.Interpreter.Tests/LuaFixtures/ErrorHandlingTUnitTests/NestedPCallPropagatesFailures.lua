-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/ErrorHandlingTUnitTests.cs:73
-- @test: ErrorHandlingTUnitTests.NestedPCallPropagatesFailures
function try(fn)
                        local ok, value = pcall(fn)
                        if ok then
                            return value
                        end
                        return '!'
                    end

                    function a()
                        return try(b) .. 'a'
                    end

                    function b()
                        return try(c) .. 'b'
                    end

                    function c()
                        return try(d) .. 'c'
                    end

                    function d()
                        local t = { } .. 'x'
                    end

                    return a()
