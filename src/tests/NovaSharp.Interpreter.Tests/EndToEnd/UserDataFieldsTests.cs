namespace NovaSharp.Interpreter.Tests.EndToEnd
{
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Interop;
    using NUnit.Framework;

    [TestFixture]
    public class UserDataFieldsTests
    {
        internal sealed class SomeClass
        {
            public int intProp;
            public const int ConstIntProp = 115;
            public readonly int roIntProp = 123;
            public int? nIntProp;
            public object objProp;
            public static string StaticProp;

            private string _privateProp;

            public SomeClass()
            {
                intProp = 0;
                nIntProp = null;
                objProp = null;
                _privateProp = string.Empty;
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

        private static void TestReadOnlyIntFieldGetter(InteropAccessMode opt)
        {
            string script =
                @"    
				x = myobj.RoIntProp;
				return x;";

            Script s = new();

            SomeClass obj = new() { intProp = 321 };

            UserData.UnregisterType<SomeClass>();
            UserData.RegisterType<SomeClass>(opt);

            s.Globals.Set("myobj", UserData.Create(obj));

            DynValue res = s.DoString(script);

            Assert.That(res.Type, Is.EqualTo(DataType.Number));
            Assert.That(res.Number, Is.EqualTo(123));
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

        private static void TestReadOnlyIntFieldSetter(InteropAccessMode opt)
        {
            try
            {
                string script =
                    @"    
				myobj.RoIntProp = 1;
				return myobj.RoIntProp;";

                Script s = new();

                SomeClass obj = new() { intProp = 321 };

                UserData.UnregisterType<SomeClass>();
                UserData.RegisterType<SomeClass>(opt);

                s.Globals.Set("myobj", UserData.Create(obj));

                DynValue res = s.DoString(script);

                Assert.That(res.Type, Is.EqualTo(DataType.Number));
                Assert.That(res.Number, Is.EqualTo(123));
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
				myobj.IntProp = 19;";

            Script s = new();

            SomeClass obj = new() { intProp = 321 };

            UserData.UnregisterType<SomeClass>();
            UserData.RegisterType<SomeClass>(opt);

            s.Globals.Set("myobj", UserData.Create(obj));

            Assert.That(obj.intProp, Is.EqualTo(321));

            DynValue res = s.DoString(script);

            Assert.That(obj.intProp, Is.EqualTo(19));
        }

        private static void TestNIntFieldSetter(InteropAccessMode opt)
        {
            string script =
                @"    
				myobj1.NIntProp = nil;
				myobj2.NIntProp = 19;";

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

            Assert.That(obj1.nIntProp, Is.EqualTo(null));
            Assert.That(obj2.nIntProp, Is.EqualTo(19));
        }

        private static void TestObjFieldSetter(InteropAccessMode opt)
        {
            string script =
                @"    
				myobj1.ObjProp = myobj2;
				myobj2.ObjProp = 'hello';";

            Script s = new();

            SomeClass obj1 = new() { objProp = "ciao" };
            SomeClass obj2 = new() { objProp = obj1 };

            UserData.UnregisterType<SomeClass>();
            UserData.RegisterType<SomeClass>(opt);

            s.Globals.Set("myobj1", UserData.Create(obj1));
            s.Globals.Set("myobj2", UserData.Create(obj2));

            Assert.That(obj1.objProp, Is.EqualTo("ciao"));
            Assert.That(obj2.objProp, Is.EqualTo(obj1));

            DynValue res = s.DoString(script);

            Assert.That(obj1.objProp, Is.EqualTo(obj2));
            Assert.That(obj2.objProp, Is.EqualTo("hello"));
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
        public void InteropIntFieldGetterNone()
        {
            TestIntFieldGetter(InteropAccessMode.Reflection);
        }

        [Test]
        public void InteropIntFieldGetterLazy()
        {
            TestIntFieldGetter(InteropAccessMode.LazyOptimized);
        }

        [Test]
        public void InteropIntFieldGetterPrecomputed()
        {
            TestIntFieldGetter(InteropAccessMode.Preoptimized);
        }

        [Test]
        public void InteropNIntFieldGetterNone()
        {
            TestNIntFieldGetter(InteropAccessMode.Reflection);
        }

        [Test]
        public void InteropNIntFieldGetterLazy()
        {
            TestNIntFieldGetter(InteropAccessMode.LazyOptimized);
        }

        [Test]
        public void InteropNIntFieldGetterPrecomputed()
        {
            TestNIntFieldGetter(InteropAccessMode.Preoptimized);
        }

        [Test]
        public void InteropObjFieldGetterNone()
        {
            TestObjFieldGetter(InteropAccessMode.Reflection);
        }

        [Test]
        public void InteropObjFieldGetterLazy()
        {
            TestObjFieldGetter(InteropAccessMode.LazyOptimized);
        }

        [Test]
        public void InteropObjFieldGetterPrecomputed()
        {
            TestObjFieldGetter(InteropAccessMode.Preoptimized);
        }

        [Test]
        public void InteropIntFieldSetterNone()
        {
            TestIntFieldSetter(InteropAccessMode.Reflection);
        }

        [Test]
        public void InteropIntFieldSetterLazy()
        {
            TestIntFieldSetter(InteropAccessMode.LazyOptimized);
        }

        [Test]
        public void InteropIntFieldSetterPrecomputed()
        {
            TestIntFieldSetter(InteropAccessMode.Preoptimized);
        }

        [Test]
        public void InteropNIntFieldSetterNone()
        {
            TestNIntFieldSetter(InteropAccessMode.Reflection);
        }

        [Test]
        public void InteropNIntFieldSetterLazy()
        {
            TestNIntFieldSetter(InteropAccessMode.LazyOptimized);
        }

        [Test]
        public void InteropNIntFieldSetterPrecomputed()
        {
            TestNIntFieldSetter(InteropAccessMode.Preoptimized);
        }

        [Test]
        public void InteropObjFieldSetterNone()
        {
            TestObjFieldSetter(InteropAccessMode.Reflection);
        }

        [Test]
        public void InteropObjFieldSetterLazy()
        {
            TestObjFieldSetter(InteropAccessMode.LazyOptimized);
        }

        [Test]
        public void InteropObjFieldSetterPrecomputed()
        {
            TestObjFieldSetter(InteropAccessMode.Preoptimized);
        }

        [Test]
        [ExpectedException(typeof(ScriptRuntimeException))]
        public void InteropInvalidFieldSetterNone()
        {
            TestInvalidFieldSetter(InteropAccessMode.Reflection);
        }

        [Test]
        [ExpectedException(typeof(ScriptRuntimeException))]
        public void InteropInvalidFieldSetterLazy()
        {
            TestInvalidFieldSetter(InteropAccessMode.LazyOptimized);
        }

        [Test]
        [ExpectedException(typeof(ScriptRuntimeException))]
        public void InteropInvalidFieldSetterPrecomputed()
        {
            TestInvalidFieldSetter(InteropAccessMode.Preoptimized);
        }

        [Test]
        public void InteropStaticFieldAccessNone()
        {
            TestStaticFieldAccess(InteropAccessMode.Reflection);
        }

        [Test]
        public void InteropStaticFieldAccessLazy()
        {
            TestStaticFieldAccess(InteropAccessMode.LazyOptimized);
        }

        [Test]
        public void InteropStaticFieldAccessPrecomputed()
        {
            TestStaticFieldAccess(InteropAccessMode.Preoptimized);
        }

        [Test]
        public void InteropIntFieldSetterWithSimplifiedSyntax()
        {
            string script =
                @"    
				myobj.IntProp = 19;";

            Script s = new();

            SomeClass obj = new() { intProp = 321 };

            UserData.UnregisterType<SomeClass>();
            UserData.RegisterType<SomeClass>();

            s.Globals["myobj"] = obj;

            Assert.That(obj.intProp, Is.EqualTo(321));

            DynValue res = s.DoString(script);

            Assert.That(s.Globals["myobj"], Is.EqualTo(obj));
            Assert.That(obj.intProp, Is.EqualTo(19));
        }

        [Test]
        public void InteropConstIntFieldGetterNone()
        {
            TestConstIntFieldGetter(InteropAccessMode.Reflection);
        }

        [Test]
        public void InteropConstIntFieldGetterLazy()
        {
            TestConstIntFieldGetter(InteropAccessMode.LazyOptimized);
        }

        [Test]
        public void InteropConstIntFieldGetterPrecomputed()
        {
            TestConstIntFieldGetter(InteropAccessMode.Preoptimized);
        }

        [Test]
        public void InteropConstIntFieldSetterNone()
        {
            TestConstIntFieldSetter(InteropAccessMode.Reflection);
        }

        [Test]
        public void InteropConstIntFieldSetterLazy()
        {
            TestConstIntFieldSetter(InteropAccessMode.LazyOptimized);
        }

        [Test]
        public void InteropConstIntFieldSetterPrecomputed()
        {
            TestConstIntFieldSetter(InteropAccessMode.Preoptimized);
        }

        [Test]
        public void InteropReadOnlyIntFieldGetterNone()
        {
            TestReadOnlyIntFieldGetter(InteropAccessMode.Reflection);
        }

        [Test]
        public void InteropReadOnlyIntFieldGetterLazy()
        {
            TestReadOnlyIntFieldGetter(InteropAccessMode.LazyOptimized);
        }

        [Test]
        public void InteropReadOnlyIntFieldGetterPrecomputed()
        {
            TestReadOnlyIntFieldGetter(InteropAccessMode.Preoptimized);
        }

        [Test]
        public void InteropReadOnlyIntFieldSetterNone()
        {
            TestReadOnlyIntFieldSetter(InteropAccessMode.Reflection);
        }

        [Test]
        public void InteropReadOnlyIntFieldSetterLazy()
        {
            TestReadOnlyIntFieldSetter(InteropAccessMode.LazyOptimized);
        }

        [Test]
        public void InteropReadOnlyIntFieldSetterPrecomputed()
        {
            TestReadOnlyIntFieldSetter(InteropAccessMode.Preoptimized);
        }
    }
}
