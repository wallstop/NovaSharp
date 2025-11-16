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
    public class VtUserDataPropertiesTests
    {
        public struct SomeClass
        {
            public int IntProp { get; set; }
            public int? NIntProp { get; set; }
            public object ObjProp { get; set; }
            public static string StaticProp { get; set; }

            public int RoIntProp
            {
                get { return 5; }
            }
            public int RoIntProp2 { get; internal set; }

            public int WoIntProp
            {
                set { IntProp = value; }
            }
            public int WoIntProp2 { internal get; set; }

            public int GetWoIntProp2()
            {
                return WoIntProp2;
            }

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

        public void TestIntPropertyGetter(InteropAccessMode opt)
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

        public void TestNIntPropertyGetter(InteropAccessMode opt)
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

        public void TestObjPropertyGetter(InteropAccessMode opt)
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

        public void TestIntPropertySetter(InteropAccessMode opt)
        {
            string script =
                @"    
				myobj.IntProp = 19;
				return myobj.IntProp;";

            Script s = new();

            SomeClass obj = new() { IntProp = 321 };

            UserData.UnregisterType<SomeClass>();
            UserData.RegisterType<SomeClass>(opt);

            s.Globals.Set("myobj", UserData.Create(obj));

            Assert.That(obj.IntProp, Is.EqualTo(321));

            DynValue res = s.DoString(script);

            Assert.That(res.Number, Is.EqualTo(19));
        }

        public void TestNIntPropertySetter(InteropAccessMode opt)
        {
            string script =
                @"    
				myobj1.NIntProp = nil;
				myobj2.NIntProp = 19;
				return myobj1.NIntProp, myobj2.NIntProp
				";

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

            Assert.That(res.Tuple[0].Type, Is.EqualTo(DataType.Nil));
            Assert.That(res.Tuple[1].Type, Is.EqualTo(DataType.Number));
            Assert.That(res.Tuple[1].Number, Is.EqualTo(19));
        }

        public void TestInvalidPropertySetter(InteropAccessMode opt)
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

            DynValue res = s.DoString(script);

            Assert.That(obj.IntProp, Is.EqualTo(19));
        }

        public void TestStaticPropertyAccess(InteropAccessMode opt)
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

        public void TestIteratorPropertyGetter(InteropAccessMode opt)
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

        public void TestRoIntPropertyGetter(InteropAccessMode opt)
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

        public void TestRoIntProperty2Getter(InteropAccessMode opt)
        {
            string script =
                @"    
				x = myobj.RoIntProp2;
				return x;";

            Script s = new();

            SomeClass obj = new() { RoIntProp2 = 1234, WoIntProp2 = 1235 };

            UserData.UnregisterType<SomeClass>();
            UserData.RegisterType<SomeClass>(opt);

            s.Globals.Set("myobj", UserData.Create(obj));

            DynValue res = s.DoString(script);

            Assert.That(res.Type, Is.EqualTo(DataType.Number));
            Assert.That(res.Number, Is.EqualTo(1234));
        }

        public void TestRoIntPropertySetter(InteropAccessMode opt)
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

        public void TestRoIntProperty2Setter(InteropAccessMode opt)
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

        public void TestWoIntPropertySetter(InteropAccessMode opt)
        {
            string script =
                @"    
				myobj.WoIntProp = 19;
				return myobj.IntProp;
			";

            Script s = new();

            SomeClass obj = new();

            UserData.UnregisterType<SomeClass>();
            UserData.RegisterType<SomeClass>(opt);

            s.Globals.Set("myobj", UserData.Create(obj));

            DynValue res = s.DoString(script);

            Assert.That(res.Number, Is.EqualTo(19));
        }

        public void TestWoIntProperty2Setter(InteropAccessMode opt)
        {
            string script =
                @"    
				myobj.WoIntProp2 = 19;
				return myobj.GetWoIntProp2();
			";

            Script s = new();

            SomeClass obj = new();

            UserData.UnregisterType<SomeClass>();
            UserData.RegisterType<SomeClass>(opt);

            s.Globals.Set("myobj", UserData.Create(obj));

            DynValue res = s.DoString(script);

            Assert.That(res.Number, Is.EqualTo(19));
        }

        public void TestWoIntPropertyGetter(InteropAccessMode opt)
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

        public void TestWoIntProperty2Getter(InteropAccessMode opt)
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

        public void TestPropertyAccessOverrides(InteropAccessMode opt)
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
                // Assert.AreEqual(19, obj.AccessOverrProp); // can't do on value type
                return;
            }

            Assert.Fail();
        }

        [Test]
        public void VInteropIntPropertyGetterNone()
        {
            TestIntPropertyGetter(InteropAccessMode.Reflection);
        }

        [Test]
        public void VInteropIntPropertyGetterLazy()
        {
            TestIntPropertyGetter(InteropAccessMode.LazyOptimized);
        }

        [Test]
        public void VInteropIntPropertyGetterPrecomputed()
        {
            TestIntPropertyGetter(InteropAccessMode.Preoptimized);
        }

        [Test]
        public void VInteropNIntPropertyGetterNone()
        {
            TestNIntPropertyGetter(InteropAccessMode.Reflection);
        }

        [Test]
        public void VInteropNIntPropertyGetterLazy()
        {
            TestNIntPropertyGetter(InteropAccessMode.LazyOptimized);
        }

        [Test]
        public void VInteropNIntPropertyGetterPrecomputed()
        {
            TestNIntPropertyGetter(InteropAccessMode.Preoptimized);
        }

        [Test]
        public void VInteropObjPropertyGetterNone()
        {
            TestObjPropertyGetter(InteropAccessMode.Reflection);
        }

        [Test]
        public void VInteropObjPropertyGetterLazy()
        {
            TestObjPropertyGetter(InteropAccessMode.LazyOptimized);
        }

        [Test]
        public void VInteropObjPropertyGetterPrecomputed()
        {
            TestObjPropertyGetter(InteropAccessMode.Preoptimized);
        }

        [Test]
        public void VInteropIntPropertySetterNone()
        {
            TestIntPropertySetter(InteropAccessMode.Reflection);
        }

        [Test]
        public void VInteropIntPropertySetterLazy()
        {
            TestIntPropertySetter(InteropAccessMode.LazyOptimized);
        }

        [Test]
        public void VInteropIntPropertySetterPrecomputed()
        {
            TestIntPropertySetter(InteropAccessMode.Preoptimized);
        }

        [Test]
        public void VInteropNIntPropertySetterNone()
        {
            TestNIntPropertySetter(InteropAccessMode.Reflection);
        }

        [Test]
        public void VInteropNIntPropertySetterLazy()
        {
            TestNIntPropertySetter(InteropAccessMode.LazyOptimized);
        }

        [Test]
        public void VInteropNIntPropertySetterPrecomputed()
        {
            TestNIntPropertySetter(InteropAccessMode.Preoptimized);
        }

        [Test]
        [ExpectedException(typeof(ScriptRuntimeException))]
        public void VInteropInvalidPropertySetterNone()
        {
            TestInvalidPropertySetter(InteropAccessMode.Reflection);
        }

        [Test]
        [ExpectedException(typeof(ScriptRuntimeException))]
        public void VInteropInvalidPropertySetterLazy()
        {
            TestInvalidPropertySetter(InteropAccessMode.LazyOptimized);
        }

        [Test]
        [ExpectedException(typeof(ScriptRuntimeException))]
        public void VInteropInvalidPropertySetterPrecomputed()
        {
            TestInvalidPropertySetter(InteropAccessMode.Preoptimized);
        }

        [Test]
        public void VInteropStaticPropertyAccessNone()
        {
            TestStaticPropertyAccess(InteropAccessMode.Reflection);
        }

        [Test]
        public void VInteropStaticPropertyAccessLazy()
        {
            TestStaticPropertyAccess(InteropAccessMode.LazyOptimized);
        }

        [Test]
        public void VInteropStaticPropertyAccessPrecomputed()
        {
            TestStaticPropertyAccess(InteropAccessMode.Preoptimized);
        }

        [Test]
        public void VInteropIteratorPropertyGetterNone()
        {
            TestIteratorPropertyGetter(InteropAccessMode.Reflection);
        }

        [Test]
        public void VInteropIteratorPropertyGetterLazy()
        {
            TestIteratorPropertyGetter(InteropAccessMode.LazyOptimized);
        }

        [Test]
        public void VInteropIteratorPropertyGetterPrecomputed()
        {
            TestIteratorPropertyGetter(InteropAccessMode.Preoptimized);
        }

        [Test]
        public void VInteropRoIntPropertyGetterNone()
        {
            TestRoIntPropertyGetter(InteropAccessMode.Reflection);
        }

        [Test]
        public void VInteropRoIntPropertyGetterLazy()
        {
            TestRoIntPropertyGetter(InteropAccessMode.LazyOptimized);
        }

        [Test]
        public void VInteropRoIntPropertyGetterPrecomputed()
        {
            TestRoIntPropertyGetter(InteropAccessMode.Preoptimized);
        }

        [Test]
        public void VInteropRoIntProperty2GetterNone()
        {
            TestRoIntProperty2Getter(InteropAccessMode.Reflection);
        }

        [Test]
        public void VInteropRoIntProperty2GetterLazy()
        {
            TestRoIntProperty2Getter(InteropAccessMode.LazyOptimized);
        }

        [Test]
        public void VInteropRoIntProperty2GetterPrecomputed()
        {
            TestRoIntProperty2Getter(InteropAccessMode.Preoptimized);
        }

        [Test]
        public void VInteropRoIntPropertySetterNone()
        {
            TestRoIntPropertySetter(InteropAccessMode.Reflection);
        }

        [Test]
        public void VInteropRoIntPropertySetterLazy()
        {
            TestRoIntPropertySetter(InteropAccessMode.LazyOptimized);
        }

        [Test]
        public void VInteropRoIntPropertySetterPrecomputed()
        {
            TestRoIntPropertySetter(InteropAccessMode.Preoptimized);
        }

        [Test]
        public void VInteropRoIntProperty2SetterNone()
        {
            TestRoIntProperty2Setter(InteropAccessMode.Reflection);
        }

        [Test]
        public void VInteropRoIntProperty2SetterLazy()
        {
            TestRoIntProperty2Setter(InteropAccessMode.LazyOptimized);
        }

        [Test]
        public void VInteropRoIntProperty2SetterPrecomputed()
        {
            TestRoIntProperty2Setter(InteropAccessMode.Preoptimized);
        }

        [Test]
        public void VInteropWoIntPropertyGetterNone()
        {
            TestWoIntPropertyGetter(InteropAccessMode.Reflection);
        }

        [Test]
        public void VInteropWoIntPropertyGetterLazy()
        {
            TestWoIntPropertyGetter(InteropAccessMode.LazyOptimized);
        }

        [Test]
        public void VInteropWoIntPropertyGetterPrecomputed()
        {
            TestWoIntPropertyGetter(InteropAccessMode.Preoptimized);
        }

        [Test]
        public void VInteropWoIntProperty2GetterNone()
        {
            TestWoIntProperty2Getter(InteropAccessMode.Reflection);
        }

        [Test]
        public void VInteropWoIntProperty2GetterLazy()
        {
            TestWoIntProperty2Getter(InteropAccessMode.LazyOptimized);
        }

        [Test]
        public void VInteropWoIntProperty2GetterPrecomputed()
        {
            TestWoIntProperty2Getter(InteropAccessMode.Preoptimized);
        }

        [Test]
        public void VInteropWoIntPropertySetterNone()
        {
            TestWoIntPropertySetter(InteropAccessMode.Reflection);
        }

        [Test]
        public void VInteropWoIntPropertySetterLazy()
        {
            TestWoIntPropertySetter(InteropAccessMode.LazyOptimized);
        }

        [Test]
        public void VInteropWoIntPropertySetterPrecomputed()
        {
            TestWoIntPropertySetter(InteropAccessMode.Preoptimized);
        }

        [Test]
        public void VInteropWoIntProperty2SetterNone()
        {
            TestWoIntProperty2Setter(InteropAccessMode.Reflection);
        }

        [Test]
        public void VInteropWoIntProperty2SetterLazy()
        {
            TestWoIntProperty2Setter(InteropAccessMode.LazyOptimized);
        }

        [Test]
        public void VInteropWoIntProperty2SetterPrecomputed()
        {
            TestWoIntProperty2Setter(InteropAccessMode.Preoptimized);
        }

        [Test]
        public void VInteropPropertyAccessOverridesNone()
        {
            TestPropertyAccessOverrides(InteropAccessMode.Reflection);
        }

        [Test]
        public void VInteropPropertyAccessOverridesLazy()
        {
            TestPropertyAccessOverrides(InteropAccessMode.LazyOptimized);
        }

        [Test]
        public void VInteropPropertyAccessOverridesPrecomputed()
        {
            TestPropertyAccessOverrides(InteropAccessMode.Preoptimized);
        }

        [Test]
        public void VInteropIntPropertySetterWithSimplifiedSyntax()
        {
            string script =
                @"    
				myobj.IntProp = 19;
				return myobj.IntProp 
			";

            Script s = new();

            SomeClass obj = new() { IntProp = 321 };

            UserData.UnregisterType<SomeClass>();
            UserData.RegisterType<SomeClass>();

            s.Globals["myobj"] = obj;

            Assert.That(obj.IntProp, Is.EqualTo(321));

            DynValue res = s.DoString(script);

            Assert.That(res.Number, Is.EqualTo(19));
        }

        [Test]
        public void VInteropOutOfRangeNumber()
        {
            Script s = new();
            long big = long.MaxValue;
            DynValue v = DynValue.FromObject(s, big);
            Assert.That(v, Is.Not.Null);
        }
    }
}
