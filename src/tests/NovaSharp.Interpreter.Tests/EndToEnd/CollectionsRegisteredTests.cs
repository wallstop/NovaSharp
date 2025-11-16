namespace NovaSharp.Interpreter.Tests.EndToEnd
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Interop;
    using NUnit.Framework;

    [TestFixture]
    public class CollectionsRegisteredTests
    {
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

            public int[,] GetMultiArray()
            {
                return _multiArray;
            }

            public int[] GetArray()
            {
                return _array;
            }

            public List<RegCollItem> GetItems()
            {
                return _items;
            }

            public List<int> GetList()
            {
                return _list;
            }

            public IEnumerator<int> GetEnumerator()
            {
                return GetList().GetEnumerator();
            }
        }

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
                UserData.RegisterType<List<RegCollItem>>();
                UserData.RegisterType<List<int>>();
                UserData.RegisterType<int[]>();
                UserData.RegisterType<int[,]>();

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
                UserData.UnregisterType<List<RegCollItem>>();
                UserData.UnregisterType<List<int>>();
                UserData.UnregisterType<int[]>();
                UserData.UnregisterType<int[,]>();
            }
        }

        [Test]
        public void RegCollIteratorOnListAuto()
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
        public void RegCollIteratorOnListManual()
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
        public void RegCollIteratorOnListChangeElem()
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
        public void RegCollIteratorOnArrayAuto()
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
        public void RegCollIteratorOnArrayChangeElem()
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
        public void RegCollIteratorOnMultiDimArrayChangeElem()
        {
            Do(
                @"
				local array = o:GetMultiArray()

				array[0, 1] = array[1, 2];

				local x = 0;
				for i in array do 
					x = x + i;
				end
				return x;
			",
                (r, o) =>
                {
                    Assert.That(r.Type, Is.EqualTo(DataType.Number));
                    Assert.That(r.Number, Is.EqualTo(41));
                    Assert.That(o.GetMultiArray()[0, 0], Is.EqualTo(2));
                    Assert.That(o.GetMultiArray()[0, 1], Is.EqualTo(9));
                    Assert.That(o.GetMultiArray()[0, 2], Is.EqualTo(6));
                    Assert.That(o.GetMultiArray()[1, 0], Is.EqualTo(7));
                    Assert.That(o.GetMultiArray()[1, 1], Is.EqualTo(8));
                    Assert.That(o.GetMultiArray()[1, 2], Is.EqualTo(9));
                }
            );
        }

        [Test]
        public void RegCollIteratorOnObjListAuto()
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
        public void RegCollIteratorOnObjListManual()
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
        public void RegCollIteratorOnObjListChangeElem()
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
