#pragma warning disable CA2007
namespace NovaSharp.Interpreter.Tests.TUnit.EndToEnd
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Interop;
    using NovaSharp.Interpreter.Interop.Attributes;
    using NovaSharp.Tests.TestInfrastructure.Scopes;

    [UserDataIsolation]
    public sealed class UserDataPropertiesTUnitTests
    {
        private sealed class SomeClass
        {
            public int IntProp { get; set; }
            public int? NIntProp { get; set; }
            public object ObjProp { get; set; }
            public static string StaticProp { get; set; }
            public int ConstIntProp { get; } = 115;
            public int RoIntProp => IntProp - IntProp + 5;
            public int RoIntProp2 { get; private set; } = 1234;
            public int WoIntProp
            {
                set => IntProp = value;
            }
            public int WoIntProp2 { internal get; set; } = 1235;

            [NovaSharpVisible(false)]
            internal int AccessOverrProp
            {
                get;
                [NovaSharpVisible(true)]
                set;
            }

            public static IEnumerable<int> Numbers
            {
                get
                {
                    for (int i = 1; i <= 4; i++)
                    {
                        yield return i;
                    }
                }
            }
        }

        [global::TUnit.Core.Test]
        public Task InteropIntPropertyGetterNone()
        {
            return TestIntPropertyGetterAsync(InteropAccessMode.Reflection);
        }

        [global::TUnit.Core.Test]
        public Task InteropIntPropertyGetterLazy()
        {
            return TestIntPropertyGetterAsync(InteropAccessMode.LazyOptimized);
        }

        [global::TUnit.Core.Test]
        public Task InteropIntPropertyGetterPrecomputed()
        {
            return TestIntPropertyGetterAsync(InteropAccessMode.Preoptimized);
        }

        [global::TUnit.Core.Test]
        public Task InteropNIntPropertyGetterNone()
        {
            return TestNullableIntPropertyGetterAsync(InteropAccessMode.Reflection);
        }

        [global::TUnit.Core.Test]
        public Task InteropNIntPropertyGetterLazy()
        {
            return TestNullableIntPropertyGetterAsync(InteropAccessMode.LazyOptimized);
        }

        [global::TUnit.Core.Test]
        public Task InteropNIntPropertyGetterPrecomputed()
        {
            return TestNullableIntPropertyGetterAsync(InteropAccessMode.Preoptimized);
        }

        [global::TUnit.Core.Test]
        public Task InteropObjPropertyGetterNone()
        {
            return TestObjectPropertyGetterAsync(InteropAccessMode.Reflection);
        }

        [global::TUnit.Core.Test]
        public Task InteropObjPropertyGetterLazy()
        {
            return TestObjectPropertyGetterAsync(InteropAccessMode.LazyOptimized);
        }

        [global::TUnit.Core.Test]
        public Task InteropObjPropertyGetterPrecomputed()
        {
            return TestObjectPropertyGetterAsync(InteropAccessMode.Preoptimized);
        }

        [global::TUnit.Core.Test]
        public Task InteropIntPropertySetterNone()
        {
            return TestIntPropertySetterAsync(InteropAccessMode.Reflection);
        }

        [global::TUnit.Core.Test]
        public Task InteropIntPropertySetterLazy()
        {
            return TestIntPropertySetterAsync(InteropAccessMode.LazyOptimized);
        }

        [global::TUnit.Core.Test]
        public Task InteropIntPropertySetterPrecomputed()
        {
            return TestIntPropertySetterAsync(InteropAccessMode.Preoptimized);
        }

        [global::TUnit.Core.Test]
        public Task InteropNIntPropertySetterNone()
        {
            return TestNullableIntPropertySetterAsync(InteropAccessMode.Reflection);
        }

        [global::TUnit.Core.Test]
        public Task InteropNIntPropertySetterLazy()
        {
            return TestNullableIntPropertySetterAsync(InteropAccessMode.LazyOptimized);
        }

        [global::TUnit.Core.Test]
        public Task InteropNIntPropertySetterPrecomputed()
        {
            return TestNullableIntPropertySetterAsync(InteropAccessMode.Preoptimized);
        }

        [global::TUnit.Core.Test]
        public Task InteropObjPropertySetterNone()
        {
            return TestObjectPropertySetterAsync(InteropAccessMode.Reflection);
        }

        [global::TUnit.Core.Test]
        public Task InteropObjPropertySetterLazy()
        {
            return TestObjectPropertySetterAsync(InteropAccessMode.LazyOptimized);
        }

        [global::TUnit.Core.Test]
        public Task InteropObjPropertySetterPrecomputed()
        {
            return TestObjectPropertySetterAsync(InteropAccessMode.Preoptimized);
        }

        [global::TUnit.Core.Test]
        public Task InteropInvalidPropertySetterNone()
        {
            return TestInvalidPropertySetterAsync(InteropAccessMode.Reflection);
        }

        [global::TUnit.Core.Test]
        public Task InteropInvalidPropertySetterLazy()
        {
            return TestInvalidPropertySetterAsync(InteropAccessMode.LazyOptimized);
        }

        [global::TUnit.Core.Test]
        public Task InteropInvalidPropertySetterPrecomputed()
        {
            return TestInvalidPropertySetterAsync(InteropAccessMode.Preoptimized);
        }

        [global::TUnit.Core.Test]
        public Task InteropStaticPropertyAccessNone()
        {
            return TestStaticPropertyAccessAsync(InteropAccessMode.Reflection);
        }

        [global::TUnit.Core.Test]
        public Task InteropStaticPropertyAccessLazy()
        {
            return TestStaticPropertyAccessAsync(InteropAccessMode.LazyOptimized);
        }

        [global::TUnit.Core.Test]
        public Task InteropStaticPropertyAccessPrecomputed()
        {
            return TestStaticPropertyAccessAsync(InteropAccessMode.Preoptimized);
        }

        [global::TUnit.Core.Test]
        public Task InteropConstIntPropertyGetterNone()
        {
            return TestConstIntPropertyGetterAsync(InteropAccessMode.Reflection);
        }

        [global::TUnit.Core.Test]
        public Task InteropConstIntPropertyGetterLazy()
        {
            return TestConstIntPropertyGetterAsync(InteropAccessMode.LazyOptimized);
        }

        [global::TUnit.Core.Test]
        public Task InteropConstIntPropertyGetterPrecomputed()
        {
            return TestConstIntPropertyGetterAsync(InteropAccessMode.Preoptimized);
        }

        [global::TUnit.Core.Test]
        public Task InteropConstIntPropertySetterNone()
        {
            return TestConstIntPropertySetterAsync(InteropAccessMode.Reflection);
        }

        [global::TUnit.Core.Test]
        public Task InteropConstIntPropertySetterLazy()
        {
            return TestConstIntPropertySetterAsync(InteropAccessMode.LazyOptimized);
        }

        [global::TUnit.Core.Test]
        public Task InteropConstIntPropertySetterPrecomputed()
        {
            return TestConstIntPropertySetterAsync(InteropAccessMode.Preoptimized);
        }

        [global::TUnit.Core.Test]
        public Task InteropReadOnlyIntPropertyGetterNone()
        {
            return TestReadOnlyIntPropertyGetterAsync(InteropAccessMode.Reflection);
        }

        [global::TUnit.Core.Test]
        public Task InteropReadOnlyIntPropertyGetterLazy()
        {
            return TestReadOnlyIntPropertyGetterAsync(InteropAccessMode.LazyOptimized);
        }

        [global::TUnit.Core.Test]
        public Task InteropReadOnlyIntPropertyGetterPrecomputed()
        {
            return TestReadOnlyIntPropertyGetterAsync(InteropAccessMode.Preoptimized);
        }

        [global::TUnit.Core.Test]
        public Task InteropReadOnlyIntPropertySetterNone()
        {
            return TestReadOnlyIntPropertySetterAsync(InteropAccessMode.Reflection);
        }

        [global::TUnit.Core.Test]
        public Task InteropReadOnlyIntPropertySetterLazy()
        {
            return TestReadOnlyIntPropertySetterAsync(InteropAccessMode.LazyOptimized);
        }

        [global::TUnit.Core.Test]
        public Task InteropReadOnlyIntPropertySetterPrecomputed()
        {
            return TestReadOnlyIntPropertySetterAsync(InteropAccessMode.Preoptimized);
        }

        [global::TUnit.Core.Test]
        public Task InteropIntPropertySetterSimplifiedSyntax()
        {
            return WithRegistrationAsync(
                () => UserData.RegisterType<SomeClass>(),
                () =>
                {
                    Script script = new();
                    SomeClass obj = new() { IntProp = 321 };
                    script.Globals["myobj"] = obj;
                    script.DoString("myobj.IntProp = 19;");
                    return Task.FromResult(obj);
                },
                async obj => await Assert.That(obj.IntProp).IsEqualTo(19)
            );
        }

        private static Task TestIntPropertyGetterAsync(InteropAccessMode mode)
        {
            return WithRegistrationAsync(
                () => UserData.RegisterType<SomeClass>(mode),
                () =>
                {
                    Script script = new();
                    SomeClass obj = new() { IntProp = 321 };
                    script.Globals.Set("myobj", UserData.Create(obj));
                    return Task.FromResult(script.DoString("return myobj.IntProp;"));
                },
                async result => await EndToEndDynValueAssert.ExpectAsync(result, 321)
            );
        }

        private static Task TestNullableIntPropertyGetterAsync(InteropAccessMode mode)
        {
            return WithRegistrationAsync(
                () => UserData.RegisterType<SomeClass>(mode),
                () =>
                {
                    Script script = new();
                    SomeClass first = new() { NIntProp = 321 };
                    SomeClass second = new() { NIntProp = null };
                    script.Globals.Set("myobj1", UserData.Create(first));
                    script.Globals.Set("myobj2", UserData.Create(second));
                    return Task.FromResult(
                        script.DoString("return myobj1.NIntProp, myobj2.NIntProp;")
                    );
                },
                async result => await EndToEndDynValueAssert.ExpectAsync(result, 321, null)
            );
        }

        private static Task TestObjectPropertyGetterAsync(InteropAccessMode mode)
        {
            return WithRegistrationAsync(
                () => UserData.RegisterType<SomeClass>(mode),
                () =>
                {
                    Script script = new();
                    SomeClass obj1 = new() { ObjProp = "ciao" };
                    SomeClass obj2 = new() { ObjProp = obj1 };
                    script.Globals.Set("myobj1", UserData.Create(obj1));
                    script.Globals.Set("myobj2", UserData.Create(obj2));
                    return Task.FromResult(
                        script.DoString(
                            "return myobj1.ObjProp, myobj2.ObjProp, myobj2.ObjProp.ObjProp;"
                        )
                    );
                },
                async result =>
                    await EndToEndDynValueAssert.ExpectAsync(
                        result,
                        "ciao",
                        DataType.UserData,
                        "ciao"
                    )
            );
        }

        private static Task TestIntPropertySetterAsync(InteropAccessMode mode)
        {
            return WithRegistrationAsync(
                () => UserData.RegisterType<SomeClass>(mode),
                () =>
                {
                    Script script = new();
                    SomeClass obj = new() { IntProp = 321 };
                    script.Globals.Set("myobj", UserData.Create(obj));
                    script.DoString("myobj.IntProp = 19;");
                    return Task.FromResult(obj);
                },
                async obj => await Assert.That(obj.IntProp).IsEqualTo(19)
            );
        }

        private static Task TestNullableIntPropertySetterAsync(InteropAccessMode mode)
        {
            return WithRegistrationAsync(
                () => UserData.RegisterType<SomeClass>(mode),
                () =>
                {
                    Script script = new();
                    SomeClass first = new() { NIntProp = 321 };
                    SomeClass second = new() { NIntProp = null };
                    script.Globals.Set("myobj1", UserData.Create(first));
                    script.Globals.Set("myobj2", UserData.Create(second));
                    script.DoString("myobj1.NIntProp = nil; myobj2.NIntProp = 19;");
                    return Task.FromResult((first, second));
                },
                async tuple =>
                {
                    await Assert.That(tuple.first.NIntProp).IsNull();
                    await Assert.That(tuple.second.NIntProp).IsEqualTo(19);
                }
            );
        }

        private static Task TestObjectPropertySetterAsync(InteropAccessMode mode)
        {
            return WithRegistrationAsync(
                () => UserData.RegisterType<SomeClass>(mode),
                () =>
                {
                    Script script = new();
                    SomeClass first = new() { ObjProp = "ciao" };
                    SomeClass second = new() { ObjProp = first };
                    script.Globals.Set("myobj1", UserData.Create(first));
                    script.Globals.Set("myobj2", UserData.Create(second));
                    script.DoString("myobj1.ObjProp = myobj2; myobj2.ObjProp = 'hello';");
                    return Task.FromResult((first, second));
                },
                async tuple =>
                {
                    await Assert.That(tuple.first.ObjProp).IsEqualTo(tuple.second);
                    await Assert.That(tuple.second.ObjProp).IsEqualTo("hello");
                }
            );
        }

        private static Task TestInvalidPropertySetterAsync(InteropAccessMode mode)
        {
            return WithRegistrationAsync(
                () => UserData.RegisterType<SomeClass>(mode),
                () =>
                {
                    Script script = new();
                    SomeClass obj = new() { IntProp = 321 };
                    script.Globals.Set("myobj", UserData.Create(obj));
                    Assert.Throws<ScriptRuntimeException>(() =>
                        script.DoString("myobj.IntProp = '19';")
                    );
                    return Task.FromResult(obj);
                },
                async obj => await Assert.That(obj.IntProp).IsEqualTo(321)
            );
        }

        private static Task TestStaticPropertyAccessAsync(InteropAccessMode mode)
        {
            return WithRegistrationAsync(
                () => UserData.RegisterType<SomeClass>(mode),
                () =>
                {
                    Script script = new();
                    SomeClass.StaticProp = "qweqwe";
                    script.Globals.Set("static", UserData.CreateStatic<SomeClass>());
                    script.DoString("static.StaticProp = 'asdasd' .. static.StaticProp;");
                    return Task.FromResult(SomeClass.StaticProp);
                },
                async value => await Assert.That(value).IsEqualTo("asdasdqweqwe")
            );
        }

        private static Task TestConstIntPropertyGetterAsync(InteropAccessMode mode)
        {
            return WithRegistrationAsync(
                () => UserData.RegisterType<SomeClass>(mode),
                () =>
                {
                    Script script = new();
                    SomeClass obj = new();
                    script.Globals.Set("myobj", UserData.Create(obj));
                    return Task.FromResult(script.DoString("return myobj.ConstIntProp;"));
                },
                async result => await EndToEndDynValueAssert.ExpectAsync(result, 115)
            );
        }

        private static Task TestConstIntPropertySetterAsync(InteropAccessMode mode)
        {
            return WithRegistrationAsync(
                () => UserData.RegisterType<SomeClass>(mode),
                () =>
                {
                    Script script = new();
                    SomeClass obj = new();
                    script.Globals.Set("myobj", UserData.Create(obj));
                    ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                        script.DoString("myobj.ConstIntProp = 1; return myobj.ConstIntProp;")
                    );
                    return Task.FromResult(exception);
                },
                _ => Task.CompletedTask
            );
        }

        private static Task TestReadOnlyIntPropertyGetterAsync(InteropAccessMode mode)
        {
            return WithRegistrationAsync(
                () => UserData.RegisterType<SomeClass>(mode),
                () =>
                {
                    Script script = new();
                    SomeClass obj = new();
                    script.Globals.Set("myobj", UserData.Create(obj));
                    return Task.FromResult(script.DoString("return myobj.RoIntProp;"));
                },
                async result => await EndToEndDynValueAssert.ExpectAsync(result, 5)
            );
        }

        private static Task TestReadOnlyIntPropertySetterAsync(InteropAccessMode mode)
        {
            return WithRegistrationAsync(
                () => UserData.RegisterType<SomeClass>(mode),
                () =>
                {
                    Script script = new();
                    SomeClass obj = new();
                    script.Globals.Set("myobj", UserData.Create(obj));
                    ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                        script.DoString("myobj.RoIntProp = 1; return myobj.RoIntProp;")
                    );
                    return Task.FromResult(exception);
                },
                _ => Task.CompletedTask
            );
        }

        private static async Task WithRegistrationAsync<T>(
            Action register,
            Func<Task<T>> execute,
            Func<T, Task> asserts
        )
        {
            ArgumentNullException.ThrowIfNull(register);
            ArgumentNullException.ThrowIfNull(execute);
            ArgumentNullException.ThrowIfNull(asserts);

            using UserDataRegistrationScope registrationScope = UserDataRegistrationScope.Create();
            registrationScope.Add<SomeClass>(ensureUnregistered: true);
            registrationScope.Add<List<SomeClass>>(ensureUnregistered: true);

            register();
            T result = await execute().ConfigureAwait(false);
            try
            {
                await asserts(result).ConfigureAwait(false);
            }
            catch (ScriptRuntimeException ex)
            {
                Debug.WriteLine(ex.DecoratedMessage);
                ex.Rethrow();
                throw;
            }
        }
    }
}
#pragma warning restore CA2007
