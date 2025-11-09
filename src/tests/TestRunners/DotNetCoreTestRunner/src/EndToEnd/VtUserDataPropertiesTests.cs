namespace NovaSharp.Interpreter.Tests.EndToEnd
{
    using System.Collections.Generic;
    using Interop;
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

        public void Test_IntPropertyGetter(InteropAccessMode opt)
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

        public void Test_NIntPropertyGetter(InteropAccessMode opt)
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

        public void Test_ObjPropertyGetter(InteropAccessMode opt)
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

        public void Test_IntPropertySetter(InteropAccessMode opt)
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

        public void Test_NIntPropertySetter(InteropAccessMode opt)
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

        public void Test_InvalidPropertySetter(InteropAccessMode opt)
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

        public void Test_StaticPropertyAccess(InteropAccessMode opt)
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

        public void Test_IteratorPropertyGetter(InteropAccessMode opt)
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

        public void Test_RoIntPropertyGetter(InteropAccessMode opt)
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

        public void Test_RoIntProperty2Getter(InteropAccessMode opt)
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

        public void Test_RoIntPropertySetter(InteropAccessMode opt)
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

        public void Test_RoIntProperty2Setter(InteropAccessMode opt)
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

        public void Test_WoIntPropertySetter(InteropAccessMode opt)
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

        public void Test_WoIntProperty2Setter(InteropAccessMode opt)
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

        public void Test_WoIntPropertyGetter(InteropAccessMode opt)
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

        public void Test_WoIntProperty2Getter(InteropAccessMode opt)
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

        public void Test_PropertyAccessOverrides(InteropAccessMode opt)
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
        public void VInterop_IntPropertyGetter_None()
        {
            Test_IntPropertyGetter(InteropAccessMode.Reflection);
        }

        [Test]
        public void VInterop_IntPropertyGetter_Lazy()
        {
            Test_IntPropertyGetter(InteropAccessMode.LazyOptimized);
        }

        [Test]
        public void VInterop_IntPropertyGetter_Precomputed()
        {
            Test_IntPropertyGetter(InteropAccessMode.Preoptimized);
        }

        [Test]
        public void VInterop_NIntPropertyGetter_None()
        {
            Test_NIntPropertyGetter(InteropAccessMode.Reflection);
        }

        [Test]
        public void VInterop_NIntPropertyGetter_Lazy()
        {
            Test_NIntPropertyGetter(InteropAccessMode.LazyOptimized);
        }

        [Test]
        public void VInterop_NIntPropertyGetter_Precomputed()
        {
            Test_NIntPropertyGetter(InteropAccessMode.Preoptimized);
        }

        [Test]
        public void VInterop_ObjPropertyGetter_None()
        {
            Test_ObjPropertyGetter(InteropAccessMode.Reflection);
        }

        [Test]
        public void VInterop_ObjPropertyGetter_Lazy()
        {
            Test_ObjPropertyGetter(InteropAccessMode.LazyOptimized);
        }

        [Test]
        public void VInterop_ObjPropertyGetter_Precomputed()
        {
            Test_ObjPropertyGetter(InteropAccessMode.Preoptimized);
        }

        [Test]
        public void VInterop_IntPropertySetter_None()
        {
            Test_IntPropertySetter(InteropAccessMode.Reflection);
        }

        [Test]
        public void VInterop_IntPropertySetter_Lazy()
        {
            Test_IntPropertySetter(InteropAccessMode.LazyOptimized);
        }

        [Test]
        public void VInterop_IntPropertySetter_Precomputed()
        {
            Test_IntPropertySetter(InteropAccessMode.Preoptimized);
        }

        [Test]
        public void VInterop_NIntPropertySetter_None()
        {
            Test_NIntPropertySetter(InteropAccessMode.Reflection);
        }

        [Test]
        public void VInterop_NIntPropertySetter_Lazy()
        {
            Test_NIntPropertySetter(InteropAccessMode.LazyOptimized);
        }

        [Test]
        public void VInterop_NIntPropertySetter_Precomputed()
        {
            Test_NIntPropertySetter(InteropAccessMode.Preoptimized);
        }

        [Test]
        [ExpectedException(typeof(ScriptRuntimeException))]
        public void VInterop_InvalidPropertySetter_None()
        {
            Test_InvalidPropertySetter(InteropAccessMode.Reflection);
        }

        [Test]
        [ExpectedException(typeof(ScriptRuntimeException))]
        public void VInterop_InvalidPropertySetter_Lazy()
        {
            Test_InvalidPropertySetter(InteropAccessMode.LazyOptimized);
        }

        [Test]
        [ExpectedException(typeof(ScriptRuntimeException))]
        public void VInterop_InvalidPropertySetter_Precomputed()
        {
            Test_InvalidPropertySetter(InteropAccessMode.Preoptimized);
        }

        [Test]
        public void VInterop_StaticPropertyAccess_None()
        {
            Test_StaticPropertyAccess(InteropAccessMode.Reflection);
        }

        [Test]
        public void VInterop_StaticPropertyAccess_Lazy()
        {
            Test_StaticPropertyAccess(InteropAccessMode.LazyOptimized);
        }

        [Test]
        public void VInterop_StaticPropertyAccess_Precomputed()
        {
            Test_StaticPropertyAccess(InteropAccessMode.Preoptimized);
        }

        [Test]
        public void VInterop_IteratorPropertyGetter_None()
        {
            Test_IteratorPropertyGetter(InteropAccessMode.Reflection);
        }

        [Test]
        public void VInterop_IteratorPropertyGetter_Lazy()
        {
            Test_IteratorPropertyGetter(InteropAccessMode.LazyOptimized);
        }

        [Test]
        public void VInterop_IteratorPropertyGetter_Precomputed()
        {
            Test_IteratorPropertyGetter(InteropAccessMode.Preoptimized);
        }

        [Test]
        public void VInterop_RoIntPropertyGetter_None()
        {
            Test_RoIntPropertyGetter(InteropAccessMode.Reflection);
        }

        [Test]
        public void VInterop_RoIntPropertyGetter_Lazy()
        {
            Test_RoIntPropertyGetter(InteropAccessMode.LazyOptimized);
        }

        [Test]
        public void VInterop_RoIntPropertyGetter_Precomputed()
        {
            Test_RoIntPropertyGetter(InteropAccessMode.Preoptimized);
        }

        [Test]
        public void VInterop_RoIntProperty2Getter_None()
        {
            Test_RoIntProperty2Getter(InteropAccessMode.Reflection);
        }

        [Test]
        public void VInterop_RoIntProperty2Getter_Lazy()
        {
            Test_RoIntProperty2Getter(InteropAccessMode.LazyOptimized);
        }

        [Test]
        public void VInterop_RoIntProperty2Getter_Precomputed()
        {
            Test_RoIntProperty2Getter(InteropAccessMode.Preoptimized);
        }

        [Test]
        public void VInterop_RoIntPropertySetter_None()
        {
            Test_RoIntPropertySetter(InteropAccessMode.Reflection);
        }

        [Test]
        public void VInterop_RoIntPropertySetter_Lazy()
        {
            Test_RoIntPropertySetter(InteropAccessMode.LazyOptimized);
        }

        [Test]
        public void VInterop_RoIntPropertySetter_Precomputed()
        {
            Test_RoIntPropertySetter(InteropAccessMode.Preoptimized);
        }

        [Test]
        public void VInterop_RoIntProperty2Setter_None()
        {
            Test_RoIntProperty2Setter(InteropAccessMode.Reflection);
        }

        [Test]
        public void VInterop_RoIntProperty2Setter_Lazy()
        {
            Test_RoIntProperty2Setter(InteropAccessMode.LazyOptimized);
        }

        [Test]
        public void VInterop_RoIntProperty2Setter_Precomputed()
        {
            Test_RoIntProperty2Setter(InteropAccessMode.Preoptimized);
        }

        [Test]
        public void VInterop_WoIntPropertyGetter_None()
        {
            Test_WoIntPropertyGetter(InteropAccessMode.Reflection);
        }

        [Test]
        public void VInterop_WoIntPropertyGetter_Lazy()
        {
            Test_WoIntPropertyGetter(InteropAccessMode.LazyOptimized);
        }

        [Test]
        public void VInterop_WoIntPropertyGetter_Precomputed()
        {
            Test_WoIntPropertyGetter(InteropAccessMode.Preoptimized);
        }

        [Test]
        public void VInterop_WoIntProperty2Getter_None()
        {
            Test_WoIntProperty2Getter(InteropAccessMode.Reflection);
        }

        [Test]
        public void VInterop_WoIntProperty2Getter_Lazy()
        {
            Test_WoIntProperty2Getter(InteropAccessMode.LazyOptimized);
        }

        [Test]
        public void VInterop_WoIntProperty2Getter_Precomputed()
        {
            Test_WoIntProperty2Getter(InteropAccessMode.Preoptimized);
        }

        [Test]
        public void VInterop_WoIntPropertySetter_None()
        {
            Test_WoIntPropertySetter(InteropAccessMode.Reflection);
        }

        [Test]
        public void VInterop_WoIntPropertySetter_Lazy()
        {
            Test_WoIntPropertySetter(InteropAccessMode.LazyOptimized);
        }

        [Test]
        public void VInterop_WoIntPropertySetter_Precomputed()
        {
            Test_WoIntPropertySetter(InteropAccessMode.Preoptimized);
        }

        [Test]
        public void VInterop_WoIntProperty2Setter_None()
        {
            Test_WoIntProperty2Setter(InteropAccessMode.Reflection);
        }

        [Test]
        public void VInterop_WoIntProperty2Setter_Lazy()
        {
            Test_WoIntProperty2Setter(InteropAccessMode.LazyOptimized);
        }

        [Test]
        public void VInterop_WoIntProperty2Setter_Precomputed()
        {
            Test_WoIntProperty2Setter(InteropAccessMode.Preoptimized);
        }

        [Test]
        public void VInterop_PropertyAccessOverrides_None()
        {
            Test_PropertyAccessOverrides(InteropAccessMode.Reflection);
        }

        [Test]
        public void VInterop_PropertyAccessOverrides_Lazy()
        {
            Test_PropertyAccessOverrides(InteropAccessMode.LazyOptimized);
        }

        [Test]
        public void VInterop_PropertyAccessOverrides_Precomputed()
        {
            Test_PropertyAccessOverrides(InteropAccessMode.Preoptimized);
        }

        [Test]
        public void VInterop_IntPropertySetterWithSimplifiedSyntax()
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
        public void VInterop_OutOfRangeNumber()
        {
            Script s = new();
            long big = long.MaxValue;
            DynValue v = DynValue.FromObject(s, big);
            Assert.That(v, Is.Not.Null);
        }
    }
}
