-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/MetatableTUnitTests.cs:91
-- @test: MetatableTUnitTests.MetatableEqualityUsesSharedMetatable
-- @compat-notes: Uses injected variable: o1
t1a = {}
                t1b = {}
                t2  = {}
                mt1 = { __eq = function( o1, o2 ) return 'whee' end }
                mt2 = { __eq = function( o1, o2 ) return 'whee' end }
                setmetatable( t1a, mt1 )
                setmetatable( t1b, mt1 )
                setmetatable( t2,  mt2 )
                return ( t1a == t1b ), ( t1a == t2 )
