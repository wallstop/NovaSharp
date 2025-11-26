namespace NovaSharp.Interpreter.Tests.EndToEnd
{
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Interop;
    using NovaSharp.Interpreter.Interop.Attributes;
    using NUnit.Framework;

    [TestFixture]
    public class UserDataNestedTypesTests
    {
        public class SomeType
        {
            private readonly string _instanceLabel;
            private static readonly SomeNestedTypePrivate PrivateAnchor;

            public SomeType()
            {
                _instanceLabel = nameof(SomeType);
            }

            static SomeType()
            {
                PrivateAnchor = new SomeNestedTypePrivate();
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

            public class SomeNestedType
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

        public struct VSomeType
        {
            public struct SomeNestedType
            {
                public static string Get()
                {
                    return "Ciao from SomeNestedType";
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

            private static readonly SomeNestedTypePrivate PrivateAnchor =
                new SomeNestedTypePrivate();

            private struct SomeNestedTypePrivate2
            {
                public static string Get()
                {
                    return "Ciao from SomeNestedTypePrivate2";
                }
            }
        }

        [Test]
        public void InteropNestedTypesPublicEnum()
        {
            Script s = new();

            UserData.RegisterType<SomeType>();

            s.Globals.Set("o", UserData.CreateStatic<SomeType>());

            DynValue res = s.DoString("return o:Get()");

            Assert.That(res.Type, Is.EqualTo(DataType.UserData));
        }

        [Test]
        public void InteropNestedTypesPublicRef()
        {
            Script s = new();

            UserData.RegisterType<SomeType>();

            s.Globals.Set("o", UserData.CreateStatic<SomeType>());

            DynValue res = s.DoString("return o.SomeNestedType:Get()");

            Assert.That(res.Type, Is.EqualTo(DataType.String));
            Assert.That(res.String, Is.EqualTo("Ciao from SomeNestedType"));
        }

        [Test]
        public void InteropNestedTypesPrivateRef()
        {
            Script s = new();

            UserData.RegisterType<SomeType>();

            s.Globals.Set("o", UserData.CreateStatic<SomeType>());

            DynValue res = s.DoString("return o.SomeNestedTypePrivate:Get()");

            Assert.That(res.Type, Is.EqualTo(DataType.String));
            Assert.That(res.String, Is.EqualTo("Ciao from SomeNestedTypePrivate"));
        }

        [Test]
        [ExpectedException(typeof(ScriptRuntimeException))]
        public void InteropNestedTypesPrivateRef2()
        {
            Script s = new();

            UserData.RegisterType<SomeType>();

            s.Globals.Set("o", UserData.CreateStatic<SomeType>());

            DynValue res = s.DoString("return o.SomeNestedTypePrivate2:Get()");

            Assert.That(res.Type, Is.EqualTo(DataType.String));
            Assert.That(res.String, Is.EqualTo("Ciao from SomeNestedTypePrivate2"));
        }

        [Test]
        public void InteropNestedTypesPublicVal()
        {
            Script s = new();

            UserData.RegisterType<VSomeType>();

            s.Globals.Set("o", UserData.CreateStatic<VSomeType>());

            DynValue res = s.DoString("return o.SomeNestedType:Get()");

            Assert.That(res.Type, Is.EqualTo(DataType.String));
            Assert.That(res.String, Is.EqualTo("Ciao from SomeNestedType"));
        }

        [Test]
        public void InteropNestedTypesPrivateVal()
        {
            Script s = new();

            UserData.RegisterType<VSomeType>();

            s.Globals.Set("o", UserData.CreateStatic<VSomeType>());

            DynValue res = s.DoString("return o.SomeNestedTypePrivate:Get()");

            Assert.That(res.Type, Is.EqualTo(DataType.String));
            Assert.That(res.String, Is.EqualTo("Ciao from SomeNestedTypePrivate"));
        }

        [Test]
        [ExpectedException(typeof(ScriptRuntimeException))]
        public void InteropNestedTypesPrivateVal2()
        {
            Script s = new();

            UserData.RegisterType<VSomeType>();

            s.Globals.Set("o", UserData.CreateStatic<VSomeType>());

            DynValue res = s.DoString("return o.SomeNestedTypePrivate2:Get()");

            Assert.That(res.Type, Is.EqualTo(DataType.String));
            Assert.That(res.String, Is.EqualTo("Ciao from SomeNestedTypePrivate2"));
        }
    }
}
