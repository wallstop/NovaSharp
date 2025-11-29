#pragma warning disable CA2007
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

    [UserDataIsolation]
    public sealed class UserDataNestedTypesTUnitTests
    {
        [global::TUnit.Core.Test]
        public async Task InteropNestedTypesPublicEnum()
        {
            Script script = new();

            UserData.RegisterType<SomeType>();
            script.Globals.Set("o", UserData.CreateStatic<SomeType>());

            DynValue result = script.DoString("return o:Get()");

            await Assert.That(result.Type).IsEqualTo(DataType.UserData);
        }

        [global::TUnit.Core.Test]
        public async Task InteropNestedTypesPublicRef()
        {
            Script script = new();

            UserData.RegisterType<SomeType>();
            script.Globals.Set("o", UserData.CreateStatic<SomeType>());

            DynValue result = script.DoString("return o.SomeNestedType:Get()");

            await Assert.That(result.Type).IsEqualTo(DataType.String);
            await Assert.That(result.String).IsEqualTo("Ciao from SomeNestedType");
        }

        [global::TUnit.Core.Test]
        public async Task InteropNestedTypesPrivateRef()
        {
            Script script = new();

            UserData.RegisterType<SomeType>();
            script.Globals.Set("o", UserData.CreateStatic<SomeType>());

            DynValue result = script.DoString("return o.SomeNestedTypePrivate:Get()");

            await Assert.That(result.Type).IsEqualTo(DataType.String);
            await Assert.That(result.String).IsEqualTo("Ciao from SomeNestedTypePrivate");
        }

        [global::TUnit.Core.Test]
        public Task InteropNestedTypesPrivateRef2()
        {
            Script script = new();

            UserData.RegisterType<SomeType>();
            script.Globals.Set("o", UserData.CreateStatic<SomeType>());

            Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("return o.SomeNestedTypePrivate2:Get()")
            );

            return Task.CompletedTask;
        }

        [global::TUnit.Core.Test]
        public async Task InteropNestedTypesPublicVal()
        {
            Script script = new();

            UserData.RegisterType<VSomeType>();
            script.Globals.Set("o", UserData.CreateStatic<VSomeType>());

            DynValue result = script.DoString("return o.SomeNestedType:Get()");

            await Assert.That(result.Type).IsEqualTo(DataType.String);
            await Assert.That(result.String).IsEqualTo("Ciao from SomeNestedType");
        }

        [global::TUnit.Core.Test]
        public async Task InteropNestedTypesPrivateVal()
        {
            Script script = new();

            UserData.RegisterType<VSomeType>();
            script.Globals.Set("o", UserData.CreateStatic<VSomeType>());

            DynValue result = script.DoString("return o.SomeNestedTypePrivate:Get()");

            await Assert.That(result.Type).IsEqualTo(DataType.String);
            await Assert.That(result.String).IsEqualTo("Ciao from SomeNestedTypePrivate");
        }

        [global::TUnit.Core.Test]
        public Task InteropNestedTypesPrivateVal2()
        {
            Script script = new();

            UserData.RegisterType<VSomeType>();
            script.Globals.Set("o", UserData.CreateStatic<VSomeType>());

            Assert.Throws<ScriptRuntimeException>(() =>
                script.DoString("return o.SomeNestedTypePrivate2:Get()")
            );

            return Task.CompletedTask;
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
#pragma warning restore CA2007
