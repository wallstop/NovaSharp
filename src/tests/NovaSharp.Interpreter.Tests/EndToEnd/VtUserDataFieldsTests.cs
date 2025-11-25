namespace NovaSharp.Interpreter.Tests.EndToEnd
{
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Interop;
    using NUnit.Framework;

#pragma warning disable 169 // unused private field

    [TestFixture]
    public class VtUserDataFieldsTests
    {
        public struct SomeClass
        {
            public int intProp;
            public const int ConstIntProp = 115;
            public int? nIntProp;
            public object objProp;
            public static string StaticProp;
            private string _privateProp;
        }

        private static void TestConstIntFieldGetter(InteropAccessMode opt)
        {
            string script =
                @"    
				x = myobj.ConstIntProp;
				return x;";

            Script s = new();

            SomeClass obj = new() { intProp = 321 };

            UserData.UnregisterType<SomeClass>();
            UserData.RegisterType<SomeClass>(opt);

            s.Globals.Set("myobj", UserData.Create(obj));

            DynValue res = s.DoString(script);

            Assert.That(res.Type, Is.EqualTo(DataType.Number));
            Assert.That(res.Number, Is.EqualTo(115));
        }

        private static void TestConstIntFieldSetter(InteropAccessMode opt)
        {
            try
            {
                string script =
                    @"    
				myobj.ConstIntProp = 1;
				return myobj.ConstIntProp;";

                Script s = new();

                SomeClass obj = new() { intProp = 321 };

                UserData.UnregisterType<SomeClass>();
                UserData.RegisterType<SomeClass>(opt);

                s.Globals.Set("myobj", UserData.Create(obj));

                DynValue res = s.DoString(script);

                Assert.That(res.Type, Is.EqualTo(DataType.Number));
                Assert.That(res.Number, Is.EqualTo(115));
            }
            catch (ScriptRuntimeException)
            {
                return;
            }

            Assert.Fail();
        }

        private static void TestIntFieldGetter(InteropAccessMode opt)
        {
            string script =
                @"    
				x = myobj.IntProp;
				return x;";

            Script s = new();

            SomeClass obj = new() { intProp = 321 };

            UserData.UnregisterType<SomeClass>();
            UserData.RegisterType<SomeClass>(opt);

            s.Globals.Set("myobj", UserData.Create(obj));

            DynValue res = s.DoString(script);

            Assert.That(res.Type, Is.EqualTo(DataType.Number));
            Assert.That(res.Number, Is.EqualTo(321));
        }

        private static void TestNIntFieldGetter(InteropAccessMode opt)
        {
            string script =
                @"    
				x = myobj1.NIntProp;
				y = myobj2.NIntProp;
				return x,y;";

            Script s = new();

            SomeClass obj1 = new() { nIntProp = 321 };
            SomeClass obj2 = new() { nIntProp = null };

            UserData.UnregisterType<SomeClass>();
            UserData.RegisterType<SomeClass>(opt);

            s.Globals.Set("myobj1", UserData.Create(obj1));
            s.Globals.Set("myobj2", UserData.Create(obj2));

            DynValue res = s.DoString(script);

            Assert.That(res.Type, Is.EqualTo(DataType.Tuple));
            Assert.That(res.Tuple[0].Number, Is.EqualTo(321.0));
            Assert.That(res.Tuple[0].Type, Is.EqualTo(DataType.Number));
            Assert.That(res.Tuple[1].Type, Is.EqualTo(DataType.Nil));
        }

        private static void TestObjFieldGetter(InteropAccessMode opt)
        {
            string script =
                @"    
				x = myobj1.ObjProp;
				y = myobj2.ObjProp;
				z = myobj2.ObjProp.ObjProp;
				return x,y,z;";

            Script s = new();

            SomeClass obj1 = new() { objProp = "ciao" };
            SomeClass obj2 = new() { objProp = obj1 };

            UserData.UnregisterType<SomeClass>();
            UserData.RegisterType<SomeClass>(opt);

            s.Globals.Set("myobj1", UserData.Create(obj1));
            s.Globals.Set("myobj2", UserData.Create(obj2));

            DynValue res = s.DoString(script);

            Assert.That(res.Type, Is.EqualTo(DataType.Tuple));
            Assert.That(res.Tuple[0].Type, Is.EqualTo(DataType.String));
            Assert.That(res.Tuple[0].String, Is.EqualTo("ciao"));
            Assert.That(res.Tuple[2].Type, Is.EqualTo(DataType.String));
            Assert.That(res.Tuple[2].String, Is.EqualTo("ciao"));
            Assert.That(res.Tuple[1].Type, Is.EqualTo(DataType.UserData));
            Assert.That(res.Tuple[1].UserData.Object, Is.EqualTo(obj1));
        }

        private static void TestIntFieldSetter(InteropAccessMode opt)
        {
            string script =
                @"    
				myobj.IntProp = 19;
				return myobj.IntProp;
				";

            Script s = new();

            SomeClass obj = new() { intProp = 321 };

            UserData.UnregisterType<SomeClass>();
            UserData.RegisterType<SomeClass>(opt);

            s.Globals.Set("myobj", UserData.Create(obj));

            Assert.That(obj.intProp, Is.EqualTo(321));

            DynValue res = s.DoString(script);

            Assert.That(res.Number, Is.EqualTo(19));

            // right! because value types do not change..
            Assert.That(obj.intProp, Is.EqualTo(321));
        }

        private static void TestNIntFieldSetter(InteropAccessMode opt)
        {
            string script =
                @"    
				myobj1.NIntProp = nil;
				myobj2.NIntProp = 19;
				return myobj1.NIntProp, myobj2.NIntProp;
			";

            Script s = new();

            SomeClass obj1 = new() { nIntProp = 321 };
            SomeClass obj2 = new() { nIntProp = null };

            UserData.UnregisterType<SomeClass>();
            UserData.RegisterType<SomeClass>(opt);

            s.Globals.Set("myobj1", UserData.Create(obj1));
            s.Globals.Set("myobj2", UserData.Create(obj2));

            Assert.That(obj1.nIntProp, Is.EqualTo(321));
            Assert.That(obj2.nIntProp, Is.EqualTo(null));

            DynValue res = s.DoString(script);

            Assert.That(res.Tuple[0].Type, Is.EqualTo(DataType.Nil));
            Assert.That(res.Tuple[1].Type, Is.EqualTo(DataType.Number));
            Assert.That(res.Tuple[1].Number, Is.EqualTo(19));

            // again.. are structs so the originals won't change
            Assert.That(obj1.nIntProp, Is.EqualTo(321));
            Assert.That(obj2.nIntProp, Is.EqualTo(null));
        }

        private static void TestInvalidFieldSetter(InteropAccessMode opt)
        {
            string script =
                @"    
				myobj.IntProp = '19';";

            Script s = new();

            SomeClass obj = new() { intProp = 321 };

            UserData.UnregisterType<SomeClass>();
            UserData.RegisterType<SomeClass>(opt);

            s.Globals.Set("myobj", UserData.Create(obj));

            Assert.That(obj.intProp, Is.EqualTo(321));

            DynValue res = s.DoString(script);

            Assert.That(obj.intProp, Is.EqualTo(19));
        }

        private static void TestStaticFieldAccess(InteropAccessMode opt)
        {
            string script =
                @"    
				static.StaticProp = 'asdasd' .. static.StaticProp;";

            Script s = new();

            SomeClass.StaticProp = "qweqwe";

            UserData.UnregisterType<SomeClass>();
            UserData.RegisterType<SomeClass>(opt);

            s.Globals.Set("static", UserData.CreateStatic<SomeClass>());

            Assert.That(SomeClass.StaticProp, Is.EqualTo("qweqwe"));

            DynValue res = s.DoString(script);

            Assert.That(SomeClass.StaticProp, Is.EqualTo("asdasdqweqwe"));
        }

        [Test]
        public void VInteropIntFieldGetterNone()
        {
            TestIntFieldGetter(InteropAccessMode.Reflection);
        }

        [Test]
        public void VInteropIntFieldGetterLazy()
        {
            TestIntFieldGetter(InteropAccessMode.LazyOptimized);
        }

        [Test]
        public void VInteropIntFieldGetterPrecomputed()
        {
            TestIntFieldGetter(InteropAccessMode.Preoptimized);
        }

        [Test]
        public void VInteropNIntFieldGetterNone()
        {
            TestNIntFieldGetter(InteropAccessMode.Reflection);
        }

        [Test]
        public void VInteropNIntFieldGetterLazy()
        {
            TestNIntFieldGetter(InteropAccessMode.LazyOptimized);
        }

        [Test]
        public void VInteropNIntFieldGetterPrecomputed()
        {
            TestNIntFieldGetter(InteropAccessMode.Preoptimized);
        }

        [Test]
        public void VInteropObjFieldGetterNone()
        {
            TestObjFieldGetter(InteropAccessMode.Reflection);
        }

        [Test]
        public void VInteropObjFieldGetterLazy()
        {
            TestObjFieldGetter(InteropAccessMode.LazyOptimized);
        }

        [Test]
        public void VInteropObjFieldGetterPrecomputed()
        {
            TestObjFieldGetter(InteropAccessMode.Preoptimized);
        }

        [Test]
        public void VInteropIntFieldSetterNone()
        {
            TestIntFieldSetter(InteropAccessMode.Reflection);
        }

        [Test]
        public void VInteropIntFieldSetterLazy()
        {
            TestIntFieldSetter(InteropAccessMode.LazyOptimized);
        }

        [Test]
        public void VInteropIntFieldSetterPrecomputed()
        {
            TestIntFieldSetter(InteropAccessMode.Preoptimized);
        }

        [Test]
        public void VInteropNIntFieldSetterNone()
        {
            TestNIntFieldSetter(InteropAccessMode.Reflection);
        }

        [Test]
        public void VInteropNIntFieldSetterLazy()
        {
            TestNIntFieldSetter(InteropAccessMode.LazyOptimized);
        }

        [Test]
        public void VInteropNIntFieldSetterPrecomputed()
        {
            TestNIntFieldSetter(InteropAccessMode.Preoptimized);
        }

        [Test]
        [ExpectedException(typeof(ScriptRuntimeException))]
        public void VInteropInvalidFieldSetterNone()
        {
            TestInvalidFieldSetter(InteropAccessMode.Reflection);
        }

        [Test]
        [ExpectedException(typeof(ScriptRuntimeException))]
        public void VInteropInvalidFieldSetterLazy()
        {
            TestInvalidFieldSetter(InteropAccessMode.LazyOptimized);
        }

        [Test]
        [ExpectedException(typeof(ScriptRuntimeException))]
        public void VInteropInvalidFieldSetterPrecomputed()
        {
            TestInvalidFieldSetter(InteropAccessMode.Preoptimized);
        }

        [Test]
        public void VInteropStaticFieldAccessNone()
        {
            TestStaticFieldAccess(InteropAccessMode.Reflection);
        }

        [Test]
        public void VInteropStaticFieldAccessLazy()
        {
            TestStaticFieldAccess(InteropAccessMode.LazyOptimized);
        }

        [Test]
        public void VInteropStaticFieldAccessPrecomputed()
        {
            TestStaticFieldAccess(InteropAccessMode.Preoptimized);
        }

        [Test]
        public void VInteropIntFieldSetterWithSimplifiedSyntax()
        {
            string script =
                @"    
				myobj.IntProp = 19;
				return myobj.IntProp;
			";

            Script s = new();

            SomeClass obj = new() { intProp = 321 };

            UserData.UnregisterType<SomeClass>();
            UserData.RegisterType<SomeClass>();

            s.Globals["myobj"] = obj;

            Assert.That(obj.intProp, Is.EqualTo(321));

            DynValue res = s.DoString(script);

            Assert.That(res.Number, Is.EqualTo(19));

            // expected behaviour
            Assert.That(obj.intProp, Is.EqualTo(321));
        }

        [Test]
        public void VInteropConstIntFieldGetterNone()
        {
            TestConstIntFieldGetter(InteropAccessMode.Reflection);
        }

        [Test]
        public void VInteropConstIntFieldGetterLazy()
        {
            TestConstIntFieldGetter(InteropAccessMode.LazyOptimized);
        }

        [Test]
        public void VInteropConstIntFieldGetterPrecomputed()
        {
            TestConstIntFieldGetter(InteropAccessMode.Preoptimized);
        }

        [Test]
        public void VInteropConstIntFieldSetterNone()
        {
            TestConstIntFieldSetter(InteropAccessMode.Reflection);
        }

        [Test]
        public void VInteropConstIntFieldSetterLazy()
        {
            TestConstIntFieldSetter(InteropAccessMode.LazyOptimized);
        }

        [Test]
        public void VInteropConstIntFieldSetterPrecomputed()
        {
            TestConstIntFieldSetter(InteropAccessMode.Preoptimized);
        }
    }
}
