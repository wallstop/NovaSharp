-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/DataTypes/ClosureTUnitTests.cs:394
-- @test: ClosureTUnitTests.MultipleNullCallArgumentsRemainLuaNil
return function(...)
                    local a, b, c, d, e = ...
                    return select('#', ...), a == nil, b == nil, c == nil, d == nil, e == nil
                end
