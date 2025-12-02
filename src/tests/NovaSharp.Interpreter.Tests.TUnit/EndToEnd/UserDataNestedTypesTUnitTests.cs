namespace NovaSharp.Interpreter.Tests.TUnit.EndToEnd
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Interop;
    using NovaSharp.Interpreter.Interop.Attributes;
    using NovaSharp.Interpreter.Tests;
    using NovaSharp.Tests.TestInfrastructure.Scopes;

    [UserDataIsolation]
    public sealed class UserDataNestedTypesTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task InteropNestedTypesPublicEnum()
        {
            await WithRegisteredTypeAsync<SomeType>(async () =>
                {
                    Script script = new();

                    script.Globals.Set("o", UserData.CreateStatic<SomeType>());

                    DynValue result = script.DoString("return o:Get()");

                    await Assert
                        .That(result.Type)
                        .IsEqualTo(DataType.UserData)
                        .ConfigureAwait(false);
                })
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task InteropNestedTypesPublicRef()
        {
            await WithRegisteredTypeAsync<SomeType>(async () =>
                {
                    Script script = new();

                    script.Globals.Set("o", UserData.CreateStatic<SomeType>());

                    DynValue result = script.DoString("return o.SomeNestedType:Get()");

                    await Assert.That(result.Type).IsEqualTo(DataType.String).ConfigureAwait(false);
                    await Assert
                        .That(result.String)
                        .IsEqualTo("Ciao from SomeNestedType")
                        .ConfigureAwait(false);
                })
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task InteropNestedTypesPrivateRef()
        {
            await WithRegisteredTypeAsync<SomeType>(async () =>
                {
                    Script script = new();

                    script.Globals.Set("o", UserData.CreateStatic<SomeType>());

                    DynValue result = script.DoString("return o.SomeNestedTypePrivate:Get()");

                    await Assert.That(result.Type).IsEqualTo(DataType.String).ConfigureAwait(false);
                    await Assert
                        .That(result.String)
                        .IsEqualTo("Ciao from SomeNestedTypePrivate")
                        .ConfigureAwait(false);
                })
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task InteropNestedTypesPrivateRef2()
        {
            await WithRegisteredTypeAsync<SomeType>(() =>
                {
                    Script script = new();

                    script.Globals.Set("o", UserData.CreateStatic<SomeType>());

                    Assert.Throws<ScriptRuntimeException>(() =>
                        script.DoString("return o.SomeNestedTypePrivate2:Get()")
                    );

                    return Task.CompletedTask;
                })
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task InteropNestedTypesPublicVal()
        {
            await WithRegisteredTypeAsync<VSomeType>(async () =>
                {
                    Script script = new();

                    script.Globals.Set("o", UserData.CreateStatic<VSomeType>());

                    DynValue result = script.DoString("return o.SomeNestedType:Get()");

                    await Assert.That(result.Type).IsEqualTo(DataType.String).ConfigureAwait(false);
                    await Assert
                        .That(result.String)
                        .IsEqualTo("Ciao from SomeNestedType")
                        .ConfigureAwait(false);
                })
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task InteropNestedTypesPrivateVal()
        {
            await WithRegisteredTypeAsync<VSomeType>(async () =>
                {
                    Script script = new();

                    script.Globals.Set("o", UserData.CreateStatic<VSomeType>());

                    DynValue result = script.DoString("return o.SomeNestedTypePrivate:Get()");

                    await Assert.That(result.Type).IsEqualTo(DataType.String).ConfigureAwait(false);
                    await Assert
                        .That(result.String)
                        .IsEqualTo("Ciao from SomeNestedTypePrivate")
                        .ConfigureAwait(false);
                })
                .ConfigureAwait(false);
        }

        [global::TUnit.Core.Test]
        public async Task InteropNestedTypesPrivateVal2()
        {
            await WithRegisteredTypeAsync<VSomeType>(() =>
                {
                    Script script = new();

                    script.Globals.Set("o", UserData.CreateStatic<VSomeType>());

                    Assert.Throws<ScriptRuntimeException>(() =>
                        script.DoString("return o.SomeNestedTypePrivate2:Get()")
                    );

                    return Task.CompletedTask;
                })
                .ConfigureAwait(false);
        }

        private static Task WithRegisteredTypeAsync<T>(Func<Task> callback)
        {
            return WithRegisteredTypeAsync(typeof(T), callback);
        }

        private static async Task WithRegisteredTypeAsync(Type type, Func<Task> callback)
        {
            using UserDataRegistrationScope registrationScope = UserDataRegistrationScope.Track(
                type,
                ensureUnregistered: true
            );
            registrationScope.RegisterType(type);
            await callback().ConfigureAwait(false);
        }
    }

    public sealed class SomeType
    {
        private readonly string _instanceLabel;

        static SomeType()
        {
            _ = new SomeNestedTypePrivate();
        }

        public SomeType()
        {
            _instanceLabel = nameof(SomeType);
        }

        internal string InstanceLabel => _instanceLabel;

        public enum NestedSampleState
        {
            Asdasdasd,
        }

        public static NestedSampleState Get()
        {
            return NestedSampleState.Asdasdasd;
        }

        [SuppressMessage(
            "Design",
            "CA1034:Do not nest type",
            Justification = "Lua interop coverage requires exposing this nested type to scripts."
        )]
        public sealed class SomeNestedType
        {
            private readonly string _instanceLabel;

            public SomeNestedType()
            {
                _instanceLabel = nameof(SomeNestedType);
            }

            internal string InstanceLabel => _instanceLabel;

            public static string Get()
            {
                return "Ciao from SomeNestedType";
            }
        }

        [NovaSharpUserData]
        private sealed class SomeNestedTypePrivate
        {
            public static string Get()
            {
                return "Ciao from SomeNestedTypePrivate";
            }
        }

        private static class SomeNestedTypePrivate2
        {
            public static string Get()
            {
                return "Ciao from SomeNestedTypePrivate2";
            }
        }
    }

    public readonly struct VSomeType : IEquatable<VSomeType>
    {
        [SuppressMessage(
            "Design",
            "CA1034:Do not nest type",
            Justification = "Lua interop coverage requires exposing this nested type to scripts."
        )]
        public readonly struct SomeNestedType : IEquatable<SomeNestedType>
        {
            public static string Get()
            {
                return "Ciao from SomeNestedType";
            }

            public bool Equals(SomeNestedType other)
            {
                return true;
            }

            public override bool Equals(object obj)
            {
                return obj is SomeNestedType;
            }

            public override int GetHashCode()
            {
                return typeof(SomeNestedType).GetHashCode();
            }

            public static bool operator ==(SomeNestedType left, SomeNestedType right)
            {
                return true;
            }

            public static bool operator !=(SomeNestedType left, SomeNestedType right)
            {
                return false;
            }
        }

        [NovaSharpUserData]
        private struct SomeNestedTypePrivate
        {
            public static string Get()
            {
                return "Ciao from SomeNestedTypePrivate";
            }
        }

        private struct SomeNestedTypePrivate2
        {
            public static string Get()
            {
                return "Ciao from SomeNestedTypePrivate2";
            }
        }

        public bool Equals(VSomeType other)
        {
            return true;
        }

        public override bool Equals(object obj)
        {
            return obj is VSomeType;
        }

        public override int GetHashCode()
        {
            return typeof(VSomeType).GetHashCode();
        }

        public static bool operator ==(VSomeType left, VSomeType right)
        {
            return true;
        }

        public static bool operator !=(VSomeType left, VSomeType right)
        {
            return false;
        }
    }
}
