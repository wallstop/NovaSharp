namespace WallstopStudios.NovaSharp.Interpreter.Tests.TUnit.EndToEnd
{
    using System.Diagnostics.CodeAnalysis;
    using System.Threading.Tasks;
    using CoreLib;
    using global::TUnit.Assertions;
    using WallstopStudios.NovaSharp.Interpreter;
    using WallstopStudios.NovaSharp.Interpreter.Compatibility;
    using WallstopStudios.NovaSharp.Interpreter.DataTypes;
    using WallstopStudios.NovaSharp.Interpreter.Errors;
    using WallstopStudios.NovaSharp.Interpreter.Interop;
    using WallstopStudios.NovaSharp.Interpreter.Modules;
    using WallstopStudios.NovaSharp.Tests.TestInfrastructure.Scopes;

    [UserDataIsolation]
    public sealed class MetatableTUnitTests
    {
        // __ipairs metamethod was added in Lua 5.2 and removed in Lua 5.3+
        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        public async Task TableIPairsWithMetatable(LuaCompatibilityVersion version)
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

            DynValue result = new Script(version, CoreModulePresets.Complete).DoString(script);
            await EndToEndDynValueAssert.ExpectAsync(result, "321").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task TableAddWithMetatable(LuaCompatibilityVersion version)
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

            Script scriptHost = new Script(version, CoreModulePresets.Complete);
            scriptHost.Globals.RegisterModuleType(typeof(TableIteratorsModule));
            scriptHost.Globals.RegisterModuleType(typeof(MetaTableModule));
            DynValue result = scriptHost.DoString(script);
            await EndToEndDynValueAssert.ExpectAsync(result, 24).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task MetatableEqualityUsesSharedMetatable(LuaCompatibilityVersion version)
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

            DynValue result = new Script(version, CoreModulePresets.Complete).DoString(script);
            await Assert.That(result.Tuple[0].Boolean).IsTrue().ConfigureAwait(false);
            await Assert.That(result.Tuple[1].Boolean).IsFalse().ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task MetatableCallReturnsValue(LuaCompatibilityVersion version)
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

            Script scriptHost = new Script(version, CoreModulePresets.Complete);
            DynValue table = scriptHost.DoString(script);
            DynValue result = scriptHost.Call(table, 3);
            await EndToEndDynValueAssert.ExpectAsync(result, 468).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task MetatableCallUpdatesState(LuaCompatibilityVersion version)
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

            DynValue result = new Script(version, CoreModulePresets.Complete).DoString(script);
            await EndToEndDynValueAssert.ExpectAsync(result, 468).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task MetatableIndexAndSetIndexFunctions(LuaCompatibilityVersion version)
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

            DynValue result = new Script(version, CoreModulePresets.Complete).DoString(script);
            await EndToEndDynValueAssert.ExpectAsync(result, "abc!bc").ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task MetatableIndexAndSetIndexBounce(LuaCompatibilityVersion version)
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

            DynValue result = new Script(version, CoreModulePresets.Complete).DoString(script);
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
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task MetatableExtensibleObjectSample(LuaCompatibilityVersion version)
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

            Script scriptHost = new Script(version, CoreModulePresets.Complete);
            using UserDataRegistrationScope registrationScope =
                UserDataRegistrationScope.Track<MyObject>(ensureUnregistered: true);
            registrationScope.RegisterType<MyObject>();
            scriptHost.Globals["o"] = new MyObject();
            DynValue result = scriptHost.DoString(script);
            await EndToEndDynValueAssert.ExpectAsync(result, 120).ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua51)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua52)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua53)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua54)]
        [global::TUnit.Core.Arguments(LuaCompatibilityVersion.Lua55)]
        public async Task IndexSetDoesNotWrackStack(LuaCompatibilityVersion version)
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

            Script script = new Script(
                version,
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
