#pragma warning disable CA2007
namespace NovaSharp.Interpreter.Tests.TUnit.EndToEnd
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Interop;

    [UserDataIsolation]
    public sealed class UserDataFieldsTUnitTests
    {
        private sealed class SomeClass
        {
            public int IntProp { get; set; }
            public const int ConstIntProp = 115;
            public int RoIntProp { get; } = 123;
            public int? NIntProp { get; set; }
            public object ObjProp { get; set; }
            public static string StaticProp { get; set; }

            private string _privateProp = string.Empty;

            public SomeClass()
            {
                TouchPrivateField();
            }

            private void TouchPrivateField()
            {
                if (_privateProp.Length == 0)
                {
                    _privateProp = _privateProp.Trim();
                }
            }
        }

        [global::TUnit.Core.Test]
        public Task InteropIntFieldGetterNone()
        {
            return TestIntFieldGetterAsync(InteropAccessMode.Reflection);
        }

        [global::TUnit.Core.Test]
        public Task InteropIntFieldGetterLazy()
        {
            return TestIntFieldGetterAsync(InteropAccessMode.LazyOptimized);
        }

        [global::TUnit.Core.Test]
        public Task InteropIntFieldGetterPrecomputed()
        {
            return TestIntFieldGetterAsync(InteropAccessMode.Preoptimized);
        }

        [global::TUnit.Core.Test]
        public Task InteropIntFieldSetterNone()
        {
            return TestIntFieldSetterAsync(InteropAccessMode.Reflection);
        }

        [global::TUnit.Core.Test]
        public Task InteropIntFieldSetterLazy()
        {
            return TestIntFieldSetterAsync(InteropAccessMode.LazyOptimized);
        }

        [global::TUnit.Core.Test]
        public Task InteropIntFieldSetterPrecomputed()
        {
            return TestIntFieldSetterAsync(InteropAccessMode.Preoptimized);
        }

        [global::TUnit.Core.Test]
        public Task InteropNIntFieldGetterNone()
        {
            return TestNullableIntFieldGetterAsync(InteropAccessMode.Reflection);
        }

        [global::TUnit.Core.Test]
        public Task InteropNIntFieldGetterLazy()
        {
            return TestNullableIntFieldGetterAsync(InteropAccessMode.LazyOptimized);
        }

        [global::TUnit.Core.Test]
        public Task InteropNIntFieldGetterPrecomputed()
        {
            return TestNullableIntFieldGetterAsync(InteropAccessMode.Preoptimized);
        }

        [global::TUnit.Core.Test]
        public Task InteropNIntFieldSetterNone()
        {
            return TestNullableIntFieldSetterAsync(InteropAccessMode.Reflection);
        }

        [global::TUnit.Core.Test]
        public Task InteropNIntFieldSetterLazy()
        {
            return TestNullableIntFieldSetterAsync(InteropAccessMode.LazyOptimized);
        }

        [global::TUnit.Core.Test]
        public Task InteropNIntFieldSetterPrecomputed()
        {
            return TestNullableIntFieldSetterAsync(InteropAccessMode.Preoptimized);
        }

        [global::TUnit.Core.Test]
        public Task InteropObjFieldGetterNone()
        {
            return TestObjectFieldGetterAsync(InteropAccessMode.Reflection);
        }

        [global::TUnit.Core.Test]
        public Task InteropObjFieldGetterLazy()
        {
            return TestObjectFieldGetterAsync(InteropAccessMode.LazyOptimized);
        }

        [global::TUnit.Core.Test]
        public Task InteropObjFieldGetterPrecomputed()
        {
            return TestObjectFieldGetterAsync(InteropAccessMode.Preoptimized);
        }

        [global::TUnit.Core.Test]
        public Task InteropObjFieldSetterNone()
        {
            return TestObjectFieldSetterAsync(InteropAccessMode.Reflection);
        }

        [global::TUnit.Core.Test]
        public Task InteropObjFieldSetterLazy()
        {
            return TestObjectFieldSetterAsync(InteropAccessMode.LazyOptimized);
        }

        [global::TUnit.Core.Test]
        public Task InteropObjFieldSetterPrecomputed()
        {
            return TestObjectFieldSetterAsync(InteropAccessMode.Preoptimized);
        }

        [global::TUnit.Core.Test]
        public Task InteropInvalidFieldSetterNone()
        {
            return TestInvalidFieldSetterAsync(InteropAccessMode.Reflection);
        }

        [global::TUnit.Core.Test]
        public Task InteropInvalidFieldSetterLazy()
        {
            return TestInvalidFieldSetterAsync(InteropAccessMode.LazyOptimized);
        }

        [global::TUnit.Core.Test]
        public Task InteropInvalidFieldSetterPrecomputed()
        {
            return TestInvalidFieldSetterAsync(InteropAccessMode.Preoptimized);
        }

        [global::TUnit.Core.Test]
        public Task InteropStaticFieldAccessNone()
        {
            return TestStaticFieldAccessAsync(InteropAccessMode.Reflection);
        }

        [global::TUnit.Core.Test]
        public Task InteropStaticFieldAccessLazy()
        {
            return TestStaticFieldAccessAsync(InteropAccessMode.LazyOptimized);
        }

        [global::TUnit.Core.Test]
        public Task InteropStaticFieldAccessPrecomputed()
        {
            return TestStaticFieldAccessAsync(InteropAccessMode.Preoptimized);
        }

        [global::TUnit.Core.Test]
        public Task InteropConstIntFieldGetterNone()
        {
            return TestConstIntFieldGetterAsync(InteropAccessMode.Reflection);
        }

        [global::TUnit.Core.Test]
        public Task InteropConstIntFieldGetterLazy()
        {
            return TestConstIntFieldGetterAsync(InteropAccessMode.LazyOptimized);
        }

        [global::TUnit.Core.Test]
        public Task InteropConstIntFieldGetterPrecomputed()
        {
            return TestConstIntFieldGetterAsync(InteropAccessMode.Preoptimized);
        }

        [global::TUnit.Core.Test]
        public Task InteropConstIntFieldSetterNone()
        {
            return TestConstIntFieldSetterAsync(InteropAccessMode.Reflection);
        }

        [global::TUnit.Core.Test]
        public Task InteropConstIntFieldSetterLazy()
        {
            return TestConstIntFieldSetterAsync(InteropAccessMode.LazyOptimized);
        }

        [global::TUnit.Core.Test]
        public Task InteropConstIntFieldSetterPrecomputed()
        {
            return TestConstIntFieldSetterAsync(InteropAccessMode.Preoptimized);
        }

        [global::TUnit.Core.Test]
        public Task InteropReadOnlyIntFieldGetterNone()
        {
            return TestReadOnlyIntFieldGetterAsync(InteropAccessMode.Reflection);
        }

        [global::TUnit.Core.Test]
        public Task InteropReadOnlyIntFieldGetterLazy()
        {
            return TestReadOnlyIntFieldGetterAsync(InteropAccessMode.LazyOptimized);
        }

        [global::TUnit.Core.Test]
        public Task InteropReadOnlyIntFieldGetterPrecomputed()
        {
            return TestReadOnlyIntFieldGetterAsync(InteropAccessMode.Preoptimized);
        }

        [global::TUnit.Core.Test]
        public Task InteropReadOnlyIntFieldSetterNone()
        {
            return TestReadOnlyIntFieldSetterAsync(InteropAccessMode.Reflection);
        }

        [global::TUnit.Core.Test]
        public Task InteropReadOnlyIntFieldSetterLazy()
        {
            return TestReadOnlyIntFieldSetterAsync(InteropAccessMode.LazyOptimized);
        }

        [global::TUnit.Core.Test]
        public Task InteropReadOnlyIntFieldSetterPrecomputed()
        {
            return TestReadOnlyIntFieldSetterAsync(InteropAccessMode.Preoptimized);
        }

        [global::TUnit.Core.Test]
        public Task InteropIntFieldSetterWithSimplifiedSyntax()
        {
            return WithRegistrationAsync(
                () => UserData.RegisterType<SomeClass>(),
                () =>
                {
                    Script script = new();
                    SomeClass obj = new() { IntProp = 321 };
                    script.Globals["myobj"] = obj;
                    DynValue result = script.DoString("myobj.IntProp = 19;");
                    return Task.FromResult((result, obj));
                },
                async tuple =>
                {
                    await Assert.That(tuple.Item2.IntProp).IsEqualTo(19);
                }
            );
        }

        private static Task TestIntFieldGetterAsync(InteropAccessMode mode)
        {
            return WithRegistrationAsync(
                () => UserData.RegisterType<SomeClass>(mode),
                () =>
                {
                    Script script = new();
                    SomeClass obj = new() { IntProp = 321 };
                    script.Globals.Set("myobj", UserData.Create(obj));
                    return Task.FromResult((script.DoString("return myobj.IntProp;"), obj));
                },
                async tuple => await EndToEndDynValueAssert.ExpectAsync(tuple.Item1, 321)
            );
        }

        private static Task TestNullableIntFieldGetterAsync(InteropAccessMode mode)
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

        private static Task TestObjectFieldGetterAsync(InteropAccessMode mode)
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

        private static Task TestIntFieldSetterAsync(InteropAccessMode mode)
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

        private static Task TestNullableIntFieldSetterAsync(InteropAccessMode mode)
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

        private static Task TestObjectFieldSetterAsync(InteropAccessMode mode)
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

        private static Task TestInvalidFieldSetterAsync(InteropAccessMode mode)
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

        private static Task TestStaticFieldAccessAsync(InteropAccessMode mode)
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

        private static Task TestConstIntFieldGetterAsync(InteropAccessMode mode)
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

        private static Task TestConstIntFieldSetterAsync(InteropAccessMode mode)
        {
            return WithRegistrationAsync(
                () => UserData.RegisterType<SomeClass>(mode),
                () =>
                {
                    Script script = new();
                    SomeClass obj = new();
                    script.Globals.Set("myobj", UserData.Create(obj));
                    return Task.FromResult(
                        script.DoString("myobj.ConstIntProp = 1; return myobj.ConstIntProp;")
                    );
                },
                async result => await EndToEndDynValueAssert.ExpectAsync(result, 115)
            );
        }

        private static Task TestReadOnlyIntFieldGetterAsync(InteropAccessMode mode)
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
                async result => await EndToEndDynValueAssert.ExpectAsync(result, 123)
            );
        }

        private static Task TestReadOnlyIntFieldSetterAsync(InteropAccessMode mode)
        {
            return WithRegistrationAsync(
                () => UserData.RegisterType<SomeClass>(mode),
                () =>
                {
                    Script script = new();
                    SomeClass obj = new();
                    script.Globals.Set("myobj", UserData.Create(obj));
                    return Task.FromResult(
                        script.DoString("myobj.RoIntProp = 1; return myobj.RoIntProp;")
                    );
                },
                async result => await EndToEndDynValueAssert.ExpectAsync(result, 123)
            );
        }

        private static Task WithRegistrationAsync<T>(
            Action register,
            Func<Task<T>> execute,
            Func<T, Task> asserts
        )
        {
            try
            {
                register();
                return ExecuteAsync();

                async Task ExecuteAsync()
                {
                    try
                    {
                        T result = await execute();
                        await asserts(result);
                    }
                    catch (ScriptRuntimeException ex)
                    {
                        Debug.WriteLine(ex.DecoratedMessage);
                        ex.Rethrow();
                        throw;
                    }
                }
            }
            finally
            {
                UserData.UnregisterType<SomeClass>();
                UserData.UnregisterType<List<SomeClass>>();
            }
        }
    }
}
#pragma warning restore CA2007
