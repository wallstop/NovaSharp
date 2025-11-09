namespace NovaSharp.Interpreter.Tests.EndToEnd
{
    using NUnit.Framework;

    [TestFixture]
    public class UserDataNestedTypesTests
    {
        public class SomeType
        {
            public enum SomeNestedEnum
            {
                Asdasdasd,
            }

            public static SomeNestedEnum Get()
            {
                return SomeNestedEnum.Asdasdasd;
            }

            public class SomeNestedType
            {
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

            private struct SomeNestedTypePrivate2
            {
                public static string Get()
                {
                    return "Ciao from SomeNestedTypePrivate2";
                }
            }
        }

        [Test]
        public void Interop_NestedTypes_Public_Enum()
        {
            Script s = new();

            UserData.RegisterType<SomeType>();

            s.Globals.Set("o", UserData.CreateStatic<SomeType>());

            DynValue res = s.DoString("return o:Get()");

            Assert.That(res.Type, Is.EqualTo(DataType.UserData));
        }

        [Test]
        public void Interop_NestedTypes_Public_Ref()
        {
            Script s = new();

            UserData.RegisterType<SomeType>();

            s.Globals.Set("o", UserData.CreateStatic<SomeType>());

            DynValue res = s.DoString("return o.SomeNestedType:Get()");

            Assert.That(res.Type, Is.EqualTo(DataType.String));
            Assert.That(res.String, Is.EqualTo("Ciao from SomeNestedType"));
        }

        [Test]
        public void Interop_NestedTypes_Private_Ref()
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
        public void Interop_NestedTypes_Private_Ref_2()
        {
            Script s = new();

            UserData.RegisterType<SomeType>();

            s.Globals.Set("o", UserData.CreateStatic<SomeType>());

            DynValue res = s.DoString("return o.SomeNestedTypePrivate2:Get()");

            Assert.That(res.Type, Is.EqualTo(DataType.String));
            Assert.That(res.String, Is.EqualTo("Ciao from SomeNestedTypePrivate2"));
        }

        [Test]
        public void Interop_NestedTypes_Public_Val()
        {
            Script s = new();

            UserData.RegisterType<VSomeType>();

            s.Globals.Set("o", UserData.CreateStatic<VSomeType>());

            DynValue res = s.DoString("return o.SomeNestedType:Get()");

            Assert.That(res.Type, Is.EqualTo(DataType.String));
            Assert.That(res.String, Is.EqualTo("Ciao from SomeNestedType"));
        }

        [Test]
        public void Interop_NestedTypes_Private_Val()
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
        public void Interop_NestedTypes_Private_Val_2()
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
