namespace NovaSharp.Interpreter.Tests.EndToEnd
{
    using System.Collections.Generic;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Interop;
    using NovaSharp.Interpreter.Interop.Attributes;
    using NUnit.Framework;

    [TestFixture]
    [Parallelizable(ParallelScope.Self)]
    [UserDataIsolation]
    public class UserDataPropertiesTests
    {
        internal sealed class SomeClass
        {
            public int IntProp { get; set; }
            public int? NIntProp { get; set; }
            public object ObjProp { get; set; }
            public static string StaticProp { get; set; }

            public int RoIntProp
            {
                get { return IntProp - IntProp + 5; }
            }
            public int RoIntProp2 { get; private set; }

            public int WoIntProp
            {
                set { IntProp = value; }
            }
            public int WoIntProp2 { internal get; set; }

            [NovaSharpVisible(false)]
            internal int AccessOverrProp
            {
                get;
                [NovaSharpVisible(true)]
                set;
            }

            public SomeClass()
            {
                RoIntProp2 = 1234;
                WoIntProp2 = 1235;
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

        private static void TestIntPropertyGetter(InteropAccessMode opt)
        {
            string script =
                @"    
				x = myobj.IntProp;
				return x;";

            Script s = new();

            SomeClass obj = new() { IntProp = 321 };

            UserData.UnregisterType<SomeClass>();
            UserData.RegisterType<SomeClass>(opt);

            s.Globals.Set("myobj", UserData.Create(obj));

            DynValue res = s.DoString(script);

            Assert.That(res.Type, Is.EqualTo(DataType.Number));
            Assert.That(res.Number, Is.EqualTo(321));
        }

        private static void TestNIntPropertyGetter(InteropAccessMode opt)
        {
            string script =
                @"    
				x = myobj1.NIntProp;
				y = myobj2.NIntProp;
				return x,y;";

            Script s = new();

            SomeClass obj1 = new() { NIntProp = 321 };
            SomeClass obj2 = new() { NIntProp = null };

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

        private static void TestObjPropertyGetter(InteropAccessMode opt)
        {
            string script =
                @"    
				x = myobj1.ObjProp;
				y = myobj2.ObjProp;
				z = myobj2.ObjProp.ObjProp;
				return x,y,z;";

            Script s = new();

            SomeClass obj1 = new() { ObjProp = "ciao" };
            SomeClass obj2 = new() { ObjProp = obj1 };

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

        private static void TestIntPropertySetter(InteropAccessMode opt)
        {
            string script =
                @"    
				myobj.IntProp = 19;";

            Script s = new();

            SomeClass obj = new() { IntProp = 321 };

            UserData.UnregisterType<SomeClass>();
            UserData.RegisterType<SomeClass>(opt);

            s.Globals.Set("myobj", UserData.Create(obj));

            Assert.That(obj.IntProp, Is.EqualTo(321));

            DynValue res = s.DoString(script);

            Assert.That(obj.IntProp, Is.EqualTo(19));
        }

        private static void TestNIntPropertySetter(InteropAccessMode opt)
        {
            string script =
                @"    
				myobj1.NIntProp = nil;
				myobj2.NIntProp = 19;";

            Script s = new();

            SomeClass obj1 = new() { NIntProp = 321 };
            SomeClass obj2 = new() { NIntProp = null };

            UserData.UnregisterType<SomeClass>();
            UserData.RegisterType<SomeClass>(opt);

            s.Globals.Set("myobj1", UserData.Create(obj1));
            s.Globals.Set("myobj2", UserData.Create(obj2));

            Assert.That(obj1.NIntProp, Is.EqualTo(321));
            Assert.That(obj2.NIntProp, Is.EqualTo(null));

            DynValue res = s.DoString(script);

            Assert.That(obj1.NIntProp, Is.EqualTo(null));
            Assert.That(obj2.NIntProp, Is.EqualTo(19));
        }

        private static void TestObjPropertySetter(InteropAccessMode opt)
        {
            string script =
                @"    
				myobj1.ObjProp = myobj2;
				myobj2.ObjProp = 'hello';";

            Script s = new();

            SomeClass obj1 = new() { ObjProp = "ciao" };
            SomeClass obj2 = new() { ObjProp = obj1 };

            UserData.UnregisterType<SomeClass>();
            UserData.RegisterType<SomeClass>(opt);

            s.Globals.Set("myobj1", UserData.Create(obj1));
            s.Globals.Set("myobj2", UserData.Create(obj2));

            Assert.That(obj1.ObjProp, Is.EqualTo("ciao"));
            Assert.That(obj2.ObjProp, Is.EqualTo(obj1));

            DynValue res = s.DoString(script);

            Assert.That(obj1.ObjProp, Is.EqualTo(obj2));
            Assert.That(obj2.ObjProp, Is.EqualTo("hello"));
        }

        private static void TestInvalidPropertySetter(InteropAccessMode opt)
        {
            string script =
                @"    
				myobj.IntProp = '19';";

            Script s = new();

            SomeClass obj = new() { IntProp = 321 };

            UserData.UnregisterType<SomeClass>();
            UserData.RegisterType<SomeClass>(opt);

            s.Globals.Set("myobj", UserData.Create(obj));

            Assert.That(obj.IntProp, Is.EqualTo(321));

            Assert.Throws<ScriptRuntimeException>(() => s.DoString(script));
            Assert.That(obj.IntProp, Is.EqualTo(321));
        }

        private static void TestStaticPropertyAccess(InteropAccessMode opt)
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

        private static void TestIteratorPropertyGetter(InteropAccessMode opt)
        {
            string script =
                @"    
				x = 0;
				for i in myobj.Numbers do
					x = x + i;
				end

				return x;";

            Script s = new();

            SomeClass obj = new();

            UserData.UnregisterType<SomeClass>();
            UserData.RegisterType<SomeClass>(opt);

            s.Globals.Set("myobj", UserData.Create(obj));

            DynValue res = s.DoString(script);

            Assert.That(res.Type, Is.EqualTo(DataType.Number));
            Assert.That(res.Number, Is.EqualTo(10));
        }

        private static void TestRoIntPropertyGetter(InteropAccessMode opt)
        {
            string script =
                @"    
				x = myobj.RoIntProp;
				return x;";

            Script s = new();

            SomeClass obj = new();

            UserData.UnregisterType<SomeClass>();
            UserData.RegisterType<SomeClass>(opt);

            s.Globals.Set("myobj", UserData.Create(obj));

            DynValue res = s.DoString(script);

            Assert.That(res.Type, Is.EqualTo(DataType.Number));
            Assert.That(res.Number, Is.EqualTo(5));
        }

        private static void TestRoIntProperty2Getter(InteropAccessMode opt)
        {
            string script =
                @"    
				x = myobj.RoIntProp2;
				return x;";

            Script s = new();

            SomeClass obj = new();

            UserData.UnregisterType<SomeClass>();
            UserData.RegisterType<SomeClass>(opt);

            s.Globals.Set("myobj", UserData.Create(obj));

            DynValue res = s.DoString(script);

            Assert.That(res.Type, Is.EqualTo(DataType.Number));
            Assert.That(res.Number, Is.EqualTo(1234));
        }

        private static void TestRoIntPropertySetter(InteropAccessMode opt)
        {
            try
            {
                string script =
                    @"    
				myobj.RoIntProp = 19;
				return myobj.RoIntProp;
			";

                Script s = new();

                SomeClass obj = new();

                UserData.UnregisterType<SomeClass>();
                UserData.RegisterType<SomeClass>(opt);

                s.Globals.Set("myobj", UserData.Create(obj));

                DynValue res = s.DoString(script);
            }
            catch (ScriptRuntimeException)
            {
                return;
            }

            Assert.Fail();
        }

        private static void TestRoIntProperty2Setter(InteropAccessMode opt)
        {
            try
            {
                string script =
                    @"    
				myobj.RoIntProp2 = 19;
				return myobj.RoIntProp2;
			";

                Script s = new();

                SomeClass obj = new();

                UserData.UnregisterType<SomeClass>();
                UserData.RegisterType<SomeClass>(opt);

                s.Globals.Set("myobj", UserData.Create(obj));

                DynValue res = s.DoString(script);
            }
            catch (ScriptRuntimeException)
            {
                return;
            }

            Assert.Fail();
        }

        private static void TestWoIntPropertySetter(InteropAccessMode opt)
        {
            string script =
                @"    
				myobj.WoIntProp = 19;
			";

            Script s = new();

            SomeClass obj = new();

            UserData.UnregisterType<SomeClass>();
            UserData.RegisterType<SomeClass>(opt);

            s.Globals.Set("myobj", UserData.Create(obj));

            DynValue res = s.DoString(script);

            Assert.That(obj.IntProp, Is.EqualTo(19));
        }

        private static void TestWoIntProperty2Setter(InteropAccessMode opt)
        {
            string script =
                @"    
				myobj.WoIntProp2 = 19;
			";

            Script s = new();

            SomeClass obj = new();

            UserData.UnregisterType<SomeClass>();
            UserData.RegisterType<SomeClass>(opt);

            s.Globals.Set("myobj", UserData.Create(obj));

            DynValue res = s.DoString(script);

            Assert.That(obj.WoIntProp2, Is.EqualTo(19));
        }

        private static void TestWoIntPropertyGetter(InteropAccessMode opt)
        {
            try
            {
                string script =
                    @"    
				x = myobj.WoIntProp;
				return x;";

                Script s = new();

                SomeClass obj = new();

                UserData.UnregisterType<SomeClass>();
                UserData.RegisterType<SomeClass>(opt);

                s.Globals.Set("myobj", UserData.Create(obj));

                DynValue res = s.DoString(script);

                Assert.That(res.Type, Is.EqualTo(DataType.Number));
                Assert.That(res.Number, Is.EqualTo(5));
            }
            catch (ScriptRuntimeException)
            {
                return;
            }

            Assert.Fail();
        }

        private static void TestWoIntProperty2Getter(InteropAccessMode opt)
        {
            try
            {
                string script =
                    @"    
				x = myobj.WoIntProp2;
				return x;";

                Script s = new();

                SomeClass obj = new();

                UserData.UnregisterType<SomeClass>();
                UserData.RegisterType<SomeClass>(opt);

                s.Globals.Set("myobj", UserData.Create(obj));

                DynValue res = s.DoString(script);

                Assert.That(res.Type, Is.EqualTo(DataType.Number));
                Assert.That(res.Number, Is.EqualTo(1234));
            }
            catch (ScriptRuntimeException)
            {
                return;
            }

            Assert.Fail();
        }

        private static void TestPropertyAccessOverrides(InteropAccessMode opt)
        {
            SomeClass obj = new();

            try
            {
                string script =
                    @"    
				myobj.AccessOverrProp = 19;
				return myobj.AccessOverrProp;
			";

                Script s = new();

                obj.AccessOverrProp = 13;

                UserData.UnregisterType<SomeClass>();
                UserData.RegisterType<SomeClass>(opt);

                s.Globals.Set("myobj", UserData.Create(obj));

                DynValue res = s.DoString(script);
            }
            catch (ScriptRuntimeException)
            {
                Assert.That(obj.AccessOverrProp, Is.EqualTo(19));
                return;
            }

            Assert.Fail();
        }

        [Test]
        public void InteropIntPropertyGetterNone()
        {
            TestIntPropertyGetter(InteropAccessMode.Reflection);
        }

        [Test]
        public void InteropIntPropertyGetterLazy()
        {
            TestIntPropertyGetter(InteropAccessMode.LazyOptimized);
        }

        [Test]
        public void InteropIntPropertyGetterPrecomputed()
        {
            TestIntPropertyGetter(InteropAccessMode.Preoptimized);
        }

        [Test]
        public void InteropNIntPropertyGetterNone()
        {
            TestNIntPropertyGetter(InteropAccessMode.Reflection);
        }

        [Test]
        public void InteropNIntPropertyGetterLazy()
        {
            TestNIntPropertyGetter(InteropAccessMode.LazyOptimized);
        }

        [Test]
        public void InteropNIntPropertyGetterPrecomputed()
        {
            TestNIntPropertyGetter(InteropAccessMode.Preoptimized);
        }

        [Test]
        public void InteropObjPropertyGetterNone()
        {
            TestObjPropertyGetter(InteropAccessMode.Reflection);
        }

        [Test]
        public void InteropObjPropertyGetterLazy()
        {
            TestObjPropertyGetter(InteropAccessMode.LazyOptimized);
        }

        [Test]
        public void InteropObjPropertyGetterPrecomputed()
        {
            TestObjPropertyGetter(InteropAccessMode.Preoptimized);
        }

        [Test]
        public void InteropIntPropertySetterNone()
        {
            TestIntPropertySetter(InteropAccessMode.Reflection);
        }

        [Test]
        public void InteropIntPropertySetterLazy()
        {
            TestIntPropertySetter(InteropAccessMode.LazyOptimized);
        }

        [Test]
        public void InteropIntPropertySetterPrecomputed()
        {
            TestIntPropertySetter(InteropAccessMode.Preoptimized);
        }

        [Test]
        public void InteropNIntPropertySetterNone()
        {
            TestNIntPropertySetter(InteropAccessMode.Reflection);
        }

        [Test]
        public void InteropNIntPropertySetterLazy()
        {
            TestNIntPropertySetter(InteropAccessMode.LazyOptimized);
        }

        [Test]
        public void InteropNIntPropertySetterPrecomputed()
        {
            TestNIntPropertySetter(InteropAccessMode.Preoptimized);
        }

        [Test]
        public void InteropObjPropertySetterNone()
        {
            TestObjPropertySetter(InteropAccessMode.Reflection);
        }

        [Test]
        public void InteropObjPropertySetterLazy()
        {
            TestObjPropertySetter(InteropAccessMode.LazyOptimized);
        }

        [Test]
        public void InteropObjPropertySetterPrecomputed()
        {
            TestObjPropertySetter(InteropAccessMode.Preoptimized);
        }

        [Test]
        public void InteropInvalidPropertySetterNone()
        {
            TestInvalidPropertySetter(InteropAccessMode.Reflection);
        }

        [Test]
        public void InteropInvalidPropertySetterLazy()
        {
            TestInvalidPropertySetter(InteropAccessMode.LazyOptimized);
        }

        [Test]
        public void InteropInvalidPropertySetterPrecomputed()
        {
            TestInvalidPropertySetter(InteropAccessMode.Preoptimized);
        }

        [Test]
        public void InteropStaticPropertyAccessNone()
        {
            TestStaticPropertyAccess(InteropAccessMode.Reflection);
        }

        [Test]
        public void InteropStaticPropertyAccessLazy()
        {
            TestStaticPropertyAccess(InteropAccessMode.LazyOptimized);
        }

        [Test]
        public void InteropStaticPropertyAccessPrecomputed()
        {
            TestStaticPropertyAccess(InteropAccessMode.Preoptimized);
        }

        [Test]
        public void InteropIteratorPropertyGetterNone()
        {
            TestIteratorPropertyGetter(InteropAccessMode.Reflection);
        }

        [Test]
        public void InteropIteratorPropertyGetterLazy()
        {
            TestIteratorPropertyGetter(InteropAccessMode.LazyOptimized);
        }

        [Test]
        public void InteropIteratorPropertyGetterPrecomputed()
        {
            TestIteratorPropertyGetter(InteropAccessMode.Preoptimized);
        }

        [Test]
        public void InteropRoIntPropertyGetterNone()
        {
            TestRoIntPropertyGetter(InteropAccessMode.Reflection);
        }

        [Test]
        public void InteropRoIntPropertyGetterLazy()
        {
            TestRoIntPropertyGetter(InteropAccessMode.LazyOptimized);
        }

        [Test]
        public void InteropRoIntPropertyGetterPrecomputed()
        {
            TestRoIntPropertyGetter(InteropAccessMode.Preoptimized);
        }

        [Test]
        public void InteropRoIntProperty2GetterNone()
        {
            TestRoIntProperty2Getter(InteropAccessMode.Reflection);
        }

        [Test]
        public void InteropRoIntProperty2GetterLazy()
        {
            TestRoIntProperty2Getter(InteropAccessMode.LazyOptimized);
        }

        [Test]
        public void InteropRoIntProperty2GetterPrecomputed()
        {
            TestRoIntProperty2Getter(InteropAccessMode.Preoptimized);
        }

        [Test]
        public void InteropRoIntPropertySetterNone()
        {
            TestRoIntPropertySetter(InteropAccessMode.Reflection);
        }

        [Test]
        public void InteropRoIntPropertySetterLazy()
        {
            TestRoIntPropertySetter(InteropAccessMode.LazyOptimized);
        }

        [Test]
        public void InteropRoIntPropertySetterPrecomputed()
        {
            TestRoIntPropertySetter(InteropAccessMode.Preoptimized);
        }

        [Test]
        public void InteropRoIntProperty2SetterNone()
        {
            TestRoIntProperty2Setter(InteropAccessMode.Reflection);
        }

        [Test]
        public void InteropRoIntProperty2SetterLazy()
        {
            TestRoIntProperty2Setter(InteropAccessMode.LazyOptimized);
        }

        [Test]
        public void InteropRoIntProperty2SetterPrecomputed()
        {
            TestRoIntProperty2Setter(InteropAccessMode.Preoptimized);
        }

        [Test]
        public void InteropWoIntPropertyGetterNone()
        {
            TestWoIntPropertyGetter(InteropAccessMode.Reflection);
        }

        [Test]
        public void InteropWoIntPropertyGetterLazy()
        {
            TestWoIntPropertyGetter(InteropAccessMode.LazyOptimized);
        }

        [Test]
        public void InteropWoIntPropertyGetterPrecomputed()
        {
            TestWoIntPropertyGetter(InteropAccessMode.Preoptimized);
        }

        [Test]
        public void InteropWoIntProperty2GetterNone()
        {
            TestWoIntProperty2Getter(InteropAccessMode.Reflection);
        }

        [Test]
        public void InteropWoIntProperty2GetterLazy()
        {
            TestWoIntProperty2Getter(InteropAccessMode.LazyOptimized);
        }

        [Test]
        public void InteropWoIntProperty2GetterPrecomputed()
        {
            TestWoIntProperty2Getter(InteropAccessMode.Preoptimized);
        }

        [Test]
        public void InteropWoIntPropertySetterNone()
        {
            TestWoIntPropertySetter(InteropAccessMode.Reflection);
        }

        [Test]
        public void InteropWoIntPropertySetterLazy()
        {
            TestWoIntPropertySetter(InteropAccessMode.LazyOptimized);
        }

        [Test]
        public void InteropWoIntPropertySetterPrecomputed()
        {
            TestWoIntPropertySetter(InteropAccessMode.Preoptimized);
        }

        [Test]
        public void InteropWoIntProperty2SetterNone()
        {
            TestWoIntProperty2Setter(InteropAccessMode.Reflection);
        }

        [Test]
        public void InteropWoIntProperty2SetterLazy()
        {
            TestWoIntProperty2Setter(InteropAccessMode.LazyOptimized);
        }

        [Test]
        public void InteropWoIntProperty2SetterPrecomputed()
        {
            TestWoIntProperty2Setter(InteropAccessMode.Preoptimized);
        }

        [Test]
        public void InteropPropertyAccessOverridesNone()
        {
            TestPropertyAccessOverrides(InteropAccessMode.Reflection);
        }

        [Test]
        public void InteropPropertyAccessOverridesLazy()
        {
            TestPropertyAccessOverrides(InteropAccessMode.LazyOptimized);
        }

        [Test]
        public void InteropPropertyAccessOverridesPrecomputed()
        {
            TestPropertyAccessOverrides(InteropAccessMode.Preoptimized);
        }

        [Test]
        public void InteropIntPropertySetterWithSimplifiedSyntax()
        {
            string script =
                @"    
				myobj.IntProp = 19;";

            Script s = new();

            SomeClass obj = new() { IntProp = 321 };

            UserData.UnregisterType<SomeClass>();
            UserData.RegisterType<SomeClass>();

            s.Globals["myobj"] = obj;

            Assert.That(obj.IntProp, Is.EqualTo(321));

            DynValue res = s.DoString(script);

            Assert.That(s.Globals["myobj"], Is.EqualTo(obj));
            Assert.That(obj.IntProp, Is.EqualTo(19));
        }

        [Test]
        public void InteropOutOfRangeNumber()
        {
            Script s = new();
            long big = long.MaxValue;
            DynValue v = DynValue.FromObject(s, big);
            Assert.That(v, Is.Not.Null);
        }
    }
}
