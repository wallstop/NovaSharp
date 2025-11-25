namespace NovaSharp.Interpreter.Tests.EndToEnd
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Interop;
    using NUnit.Framework;

    public class RegCollItem
    {
        public int value;

        public RegCollItem(int v)
        {
            value = v;
        }
    }

    public class RegCollMethods
    {
        private readonly List<RegCollItem> _items = new()
        {
            new RegCollItem(7),
            new RegCollItem(8),
            new RegCollItem(9),
        };

        private readonly List<int> _list = new() { 1, 2, 3 };
        private readonly int[] _array = new int[3] { 2, 4, 6 };

        private readonly int[,] _multiArray = new int[2, 3]
        {
            { 2, 4, 6 },
            { 7, 8, 9 },
        };

        [SuppressMessage(
            "Design",
            "CA1024:UsePropertiesWhereAppropriate",
            Justification = "Lua interop tests require method-style getters to exercise colon-call semantics."
        )]
        public int[,] GetMultiArray()
        {
            return _multiArray;
        }

        [SuppressMessage(
            "Design",
            "CA1024:UsePropertiesWhereAppropriate",
            Justification = "Lua interop tests require method-style getters to exercise colon-call semantics."
        )]
        public int[] GetArray()
        {
            return _array;
        }

        [SuppressMessage(
            "Design",
            "CA1024:UsePropertiesWhereAppropriate",
            Justification = "Lua interop tests require method-style getters to exercise colon-call semantics."
        )]
        public List<RegCollItem> GetItems()
        {
            return _items;
        }

        [SuppressMessage(
            "Design",
            "CA1024:UsePropertiesWhereAppropriate",
            Justification = "Lua interop tests require method-style getters to exercise colon-call semantics."
        )]
        public List<int> GetList()
        {
            return _list;
        }

        public IEnumerator<int> GetEnumerator()
        {
            return GetList().GetEnumerator();
        }
    }

    [TestFixture]
    public class CollectionsBaseInterfGenRegisteredTests
    {
        private void Do(string code, Action<DynValue> asserts)
        {
            Do(code, (d, o) => asserts(d));
        }

        private void Do(string code, Action<DynValue, RegCollMethods> asserts)
        {
            try
            {
                UserData.RegisterType<RegCollMethods>();
                UserData.RegisterType<RegCollItem>();
                UserData.RegisterType(typeof(IList<>));

                Script s = new();

                RegCollMethods obj = new();
                s.Globals["o"] = obj;
                s.Globals["ctor"] = UserData.CreateStatic<RegCollItem>();

                DynValue res = s.DoString(code);

                asserts(res, obj);
            }
            catch (ScriptRuntimeException ex)
            {
                Debug.WriteLine(ex.DecoratedMessage);
                ex.Rethrow();
                throw;
            }
            finally
            {
                UserData.UnregisterType<RegCollMethods>();
                UserData.UnregisterType<RegCollItem>();
                UserData.UnregisterType<Array>();
                UserData.UnregisterType(typeof(IList<>));
                UserData.UnregisterType(typeof(IList<RegCollItem>));
                UserData.UnregisterType(typeof(IList<int>));
                //UserData.UnregisterType<IEnumerable>();
            }
        }

        [Test]
        public void RegCollGenInterfIteratorOnListAuto()
        {
            Do(
                @"
				local list = o:GetList()

				local x = 0;
				for i in list do 
					x = x + i;
				end
				return x;
			",
                (r) =>
                {
                    Assert.That(r.Type, Is.EqualTo(DataType.Number));
                    Assert.That(r.Number, Is.EqualTo(6));
                }
            );
        }

        [Test]
        public void RegCollGenInterfIteratorOnListManual()
        {
            Do(
                @"
				function each(obj)
					local e = obj:GetEnumerator()
					return function()
						if e:MoveNext() then
							return e.Current
						end
					end
				end

				local list = o; 

				local x = 0;
				for i in each(list) do 
					x = x + i;
				end
				return x;

			",
                (r) =>
                {
                    Assert.That(r.Type, Is.EqualTo(DataType.Number));
                    Assert.That(r.Number, Is.EqualTo(6));
                }
            );
        }

        [Test]
        public void RegCollGenInterfIteratorOnListChangeElem()
        {
            Do(
                @"
				local list = o:GetList()

				list[1] = list[2] + list[1];

				local x = 0;
				for i in list do 
					x = x + i;
				end
				return x;
			",
                (r, o) =>
                {
                    Assert.That(r.Type, Is.EqualTo(DataType.Number));
                    Assert.That(r.Number, Is.EqualTo(9));
                    Assert.That(o.GetList()[0], Is.EqualTo(1));
                    Assert.That(o.GetList()[1], Is.EqualTo(5));
                    Assert.That(o.GetList()[2], Is.EqualTo(3));
                }
            );
        }

        [Test]
        public void RegCollGenInterfIteratorOnArrayAuto()
        {
            Do(
                @"
				local array = o:GetArray()

				local x = 0;
				for i in array do 
					x = x + i;
				end
				return x;			",
                (r) =>
                {
                    Assert.That(r.Type, Is.EqualTo(DataType.Number));
                    Assert.That(r.Number, Is.EqualTo(12));
                }
            );
        }

        [Test]
        public void RegCollGenInterfIteratorOnArrayChangeElem()
        {
            Do(
                @"
				local array = o:get_array()

				array[1] = array[2] - 1;

				local x = 0;
				for i in array do 
					x = x + i;
				end
				return x;
			",
                (r, o) =>
                {
                    Assert.That(r.Type, Is.EqualTo(DataType.Number));
                    Assert.That(r.Number, Is.EqualTo(13));
                    Assert.That(o.GetArray()[0], Is.EqualTo(2));
                    Assert.That(o.GetArray()[1], Is.EqualTo(5));
                    Assert.That(o.GetArray()[2], Is.EqualTo(6));
                }
            );
        }

        [Test]
        public void RegCollGenInterfIteratorOnObjListAuto()
        {
            Do(
                @"
				local list = o:GetItems()

				local x = 0;
				for i in list do 
					x = x + i.Value;
				end
				return x;
			",
                (r) =>
                {
                    Assert.That(r.Type, Is.EqualTo(DataType.Number));
                    Assert.That(r.Number, Is.EqualTo(24));
                }
            );
        }

        [Test]
        public void RegCollGenInterfIteratorOnObjListManual()
        {
            Do(
                @"
				function each(obj)
					local e = obj:GetEnumerator()
					return function()
						if e:MoveNext() then
							return e.Current
						end
					end
				end

				local list = o.get_items(); 

				local x = 0;
				for i in each(list) do 
					x = x + i.Value;
				end
				return x;

			",
                (r) =>
                {
                    Assert.That(r.Type, Is.EqualTo(DataType.Number));
                    Assert.That(r.Number, Is.EqualTo(24));
                }
            );
        }

        [Test]
        public void RegCollGenInterfIteratorOnObjListChangeElem()
        {
            Do(
                @"
				local list = o:GetItems()

				list[1] = ctor.__new(list[2].Value + list[1].Value);

				local x = 0;
				for i in list do 
					x = x + i.Value;
				end
				return x;
			",
                (r, o) =>
                {
                    Assert.That(r.Type, Is.EqualTo(DataType.Number));
                    Assert.That(r.Number, Is.EqualTo(7 + 17 + 9));
                    Assert.That(o.GetItems()[0].value, Is.EqualTo(7));
                    Assert.That(o.GetItems()[1].value, Is.EqualTo(17));
                    Assert.That(o.GetItems()[2].value, Is.EqualTo(9));
                }
            );
        }
    }
}
