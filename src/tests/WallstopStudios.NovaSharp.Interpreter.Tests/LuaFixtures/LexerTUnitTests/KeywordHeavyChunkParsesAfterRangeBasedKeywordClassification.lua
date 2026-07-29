-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Tree/Lexer/LexerTUnitTests.cs:153
-- @test: LexerTUnitTests.KeywordHeavyChunkParsesAfterRangeBasedKeywordClassification
local total = 0
                local values = { true, false, nil }

                local function adjust(value)
                    if value and not false then
                        return 3
                    elseif value or false then
                        return 2
                    else
                        return 1
                    end
                end

                for _, value in ipairs(values) do
                    total = total + adjust(value)
                end

                repeat
                    total = total + 1
                until total > 8

                while total < 10 do
                    total = total + 1
                end

                return total
