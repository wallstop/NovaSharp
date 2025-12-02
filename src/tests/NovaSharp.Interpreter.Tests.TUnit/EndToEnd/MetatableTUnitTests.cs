namespace NovaSharp.Interpreter.Tests.TUnit.EndToEnd
{
    using System.Diagnostics.CodeAnalysis;
    using System.Threading.Tasks;
    using CoreLib;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Interop;
    using NovaSharp.Interpreter.Modules;

    [UserDataIsolation]
    public sealed class MetatableTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task TableIPairsWithMetatable()
        {
            string script =
                @"
                test = { 2, 4, 6 }
                meta = { }
                function meta.__ipairs(t)
                    local function ripairs_it(t,i)
                        i=i-1
                        local v=t[i]
                        if v==nil then return v end
                        return i,v
                    end
                    return ripairs_it, t, #t+1
                end
                setmetatable(test, meta);
                x = '';
                for i,v in ipairs(test) do
                    x = x .. i;
                end
                return x;
                ";

            DynValue result = new Script().DoString(script);
            await EndToEndDynValueAssert.ExpectAsync(result, "321").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task TableAddWithMetatable()
        {
            string script =
                @"
                v1 = { 'aaaa' }
                v2 = { 'aaaaaa' }
                meta = { }
                function meta.__add(t1, t2)
                    local o1 = #t1[1];
                    local o2 = #t2[1];
                    return o1 * o2;
                end
                setmetatable(v1, meta);
                return v1 + v2;
                ";

            Script scriptHost = new();
            scriptHost.Globals.RegisterModuleType(typeof(TableIteratorsModule));
            scriptHost.Globals.RegisterModuleType(typeof(MetaTableModule));
            DynValue result = scriptHost.DoString(script);
            await EndToEndDynValueAssert.ExpectAsync(result, 24).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task MetatableEqualityUsesSharedMetatable()
        {
            string script =
                @"
                t1a = {}
                t1b = {}
                t2  = {}
                mt1 = { __eq = function( o1, o2 ) return 'whee' end }
                mt2 = { __eq = function( o1, o2 ) return 'whee' end }
                setmetatable( t1a, mt1 )
                setmetatable( t1b, mt1 )
                setmetatable( t2,  mt2 )
                return ( t1a == t1b ), ( t1a == t2 )
                ";

            DynValue result = new Script().DoString(script);
            await Assert.That(result.Tuple[0].Boolean).IsTrue().ConfigureAwait(false);
            await Assert.That(result.Tuple[1].Boolean).IsFalse().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task MetatableCallReturnsValue()
        {
            string script =
                @"
                t = {}
                meta = {}
                function meta.__call(f, y)
                    return 156 * y
                end
                setmetatable(t, meta);
                return t;
                ";

            Script scriptHost = new();
            DynValue table = scriptHost.DoString(script);
            DynValue result = scriptHost.Call(table, 3);
            await EndToEndDynValueAssert.ExpectAsync(result, 468).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task MetatableCallUpdatesState()
        {
            string script =
                @"
                t = {}
                meta = {}
                x = 0;
                function meta.__call(f, y)
                    x = 156 * y;
                end
                setmetatable(t, meta);
                t(3);
                return x;
                ";

            DynValue result = new Script().DoString(script);
            await EndToEndDynValueAssert.ExpectAsync(result, 468).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task MetatableIndexAndSetIndexFunctions()
        {
            string script =
                @"
                T = { a = 'a', b = 'b', c = 'c' };
                t = { };
                m = { };
                s = '';
                function m.__index(obj, idx)
                    return T[idx];
                end
                function m.__newindex(obj, idx, val)
                    T[idx] = val;
                end
                setmetatable(t, m);
                s = s .. t.a .. t.b .. t.c;
                t.a = '!';
                s = s .. t.a .. t.b .. t.c;
                return s;
                ";

            DynValue result = new Script().DoString(script);
            await EndToEndDynValueAssert.ExpectAsync(result, "abc!bc").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task MetatableIndexAndSetIndexBounce()
        {
            string script =
                @"
                T = { a = 'a', b = 'b', c = 'c' };
                t = { };
                m = { __index = T, __newindex = T };
                s = '';
                setmetatable(t, m);
                s = s .. t.a .. t.b .. t.c;
                t.a = '!';
                s = s .. t.a .. t.b .. t.c;
                return s;
                ";

            DynValue result = new Script().DoString(script);
            await EndToEndDynValueAssert.ExpectAsync(result, "abc!bc").ConfigureAwait(false);
        }

        internal sealed class MyObject
        {
            private readonly int _value = 10;

            [SuppressMessage(
                "Design",
                "CA1024:UsePropertiesWhereAppropriate",
                Justification = "Metatable sample ensures callable Lua getter semantics."
            )]
            public int GetSomething()
            {
                return _value;
            }
        }

        [global::TUnit.Core.Test]
        public async Task MetatableExtensibleObjectSample()
        {
            string script =
                @"
                extensibleObjectMeta = {
                    __index = function(t, name)
                        local obj = rawget(t, 'wrappedobj');
                        if (obj) then return obj[name]; end
                    end
                }
                myobj = { wrappedobj = o };
                setmetatable(myobj, extensibleObjectMeta);
                function myobj.extended()
                    return 12;
                end
                return myobj.extended() * myobj.getSomething();
                ";

            Script scriptHost = new();
            UserData.RegisterType<MyObject>();
            scriptHost.Globals["o"] = new MyObject();
            DynValue result = scriptHost.DoString(script);
            await EndToEndDynValueAssert.ExpectAsync(result, 120).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task IndexSetDoesNotWrackStack()
        {
            string scriptCode =
                @"
                local aClass = {}
                setmetatable(aClass, {__newindex = function() end, __index = function() end })
                local p = {a = 1, b = 2}
                for x , v in pairs(p) do
                    print (x, v)
                    aClass[x] = v
                end
                ";

            Script script = new(
                CoreModules.Basic
                    | CoreModules.Table
                    | CoreModules.TableIterators
                    | CoreModules.Metatables
            );

            ScriptRuntimeException exception = null;
            try
            {
                script.DoString(scriptCode);
            }
            catch (ScriptRuntimeException ex)
            {
                exception = ex;
            }

            await Assert.That(exception).IsNull().ConfigureAwait(false);
        }
    }
}
