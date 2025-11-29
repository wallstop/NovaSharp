#pragma warning disable CA2007
#pragma warning disable CA1814
namespace NovaSharp.Interpreter.Tests.TUnit.EndToEnd
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Interop;

    [UserDataIsolation]
    public sealed class CollectionsBaseGenRegisteredTUnitTests
    {
        private sealed class RegCollItem
        {
            public RegCollItem(int value)
            {
                Value = value;
            }

            public int Value { get; set; }
        }

        private sealed class RegCollMethods
        {
            private readonly List<RegCollItem> _items = new()
            {
                new RegCollItem(7),
                new RegCollItem(8),
                new RegCollItem(9),
            };

            private readonly List<int> _list = new() { 1, 2, 3 };
            private readonly int[] _array = new[] { 2, 4, 6 };

            [SuppressMessage(
                "Performance",
                "CA1814:Prefer jagged arrays over multidimensional",
                Justification = "Interop coverage requires rectangular arrays."
            )]
            private readonly int[,] _multiArray = new int[2, 3]
            {
                { 2, 4, 6 },
                { 7, 8, 9 },
            };

            [SuppressMessage(
                "Design",
                "CA1024:UsePropertiesWhereAppropriate",
                Justification = "Lua interop coverage relies on method syntax."
            )]
            public int[,] GetMultiArray()
            {
                return _multiArray;
            }

            [SuppressMessage(
                "Design",
                "CA1024",
                Justification = "Lua interop coverage requires method syntax."
            )]
            public int[] GetArray()
            {
                return _array;
            }

            [SuppressMessage(
                "Design",
                "CA1024",
                Justification = "Lua interop coverage requires method syntax."
            )]
            public List<RegCollItem> GetItems()
            {
                return _items;
            }

            [SuppressMessage(
                "Design",
                "CA1024",
                Justification = "Lua interop coverage requires method syntax."
            )]
            public List<int> GetList()
            {
                return _list;
            }

            public List<int>.Enumerator GetEnumerator() => _list.GetEnumerator();
        }

        [global::TUnit.Core.Test]
        public Task RegCollGenListExtMethLast()
        {
            return RunScriptAsync(
                @"
                local list = o:GetList()
                return list:Last();
                ",
                async result =>
                {
                    await Assert.That(result.Type).IsEqualTo(DataType.Number);
                    await Assert.That(result.Number).IsEqualTo(3);
                }
            );
        }

        [global::TUnit.Core.Test]
        public Task RegCollGenListExtMethSum()
        {
            return RunScriptAsync(
                @"
                local list = o:GetList()
                return list:Sum();
                ",
                async result =>
                {
                    await Assert.That(result.Type).IsEqualTo(DataType.Number);
                    await Assert.That(result.Number).IsEqualTo(6);
                }
            );
        }

        [global::TUnit.Core.Test]
        public Task RegCollGenIteratorOnListAuto()
        {
            return RunScriptAsync(
                @"
                local list = o:GetList()
                local x = 0;
                for i in list do
                    x = x + i;
                end
                return x;
                ",
                async result =>
                {
                    await Assert.That(result.Type).IsEqualTo(DataType.Number);
                    await Assert.That(result.Number).IsEqualTo(6);
                }
            );
        }

        [global::TUnit.Core.Test]
        public Task RegCollGenIteratorOnListManual()
        {
            return RunScriptAsync(
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
                async result =>
                {
                    await Assert.That(result.Type).IsEqualTo(DataType.Number);
                    await Assert.That(result.Number).IsEqualTo(6);
                }
            );
        }

        [global::TUnit.Core.Test]
        public Task RegCollGenIteratorOnListChangeElem()
        {
            return RunScriptAsync(
                @"
                local list = o:GetList()
                list[1] = list[2] + list[1];
                local x = 0;
                for i in list do
                    x = x + i;
                end
                return x;
                ",
                async (result, host) =>
                {
                    await Assert.That(result.Type).IsEqualTo(DataType.Number);
                    await Assert.That(result.Number).IsEqualTo(9);
                    await Assert.That(host.GetList()[0]).IsEqualTo(1);
                    await Assert.That(host.GetList()[1]).IsEqualTo(5);
                    await Assert.That(host.GetList()[2]).IsEqualTo(3);
                }
            );
        }

        [global::TUnit.Core.Test]
        public Task RegCollGenIteratorOnArrayAuto()
        {
            return RunScriptAsync(
                @"
                local array = o:GetArray()
                local x = 0;
                for i in array do
                    x = x + i;
                end
                return x;
                ",
                async result =>
                {
                    await Assert.That(result.Type).IsEqualTo(DataType.Number);
                    await Assert.That(result.Number).IsEqualTo(12);
                }
            );
        }

        [global::TUnit.Core.Test]
        public Task RegCollGenIteratorOnArrayChangeElem()
        {
            return RunScriptAsync(
                @"
                local array = o:get_array()
                array[1] = array[2] - 1;
                local x = 0;
                for i in array do
                    x = x + i;
                end
                return x;
                ",
                async (result, host) =>
                {
                    await Assert.That(result.Type).IsEqualTo(DataType.Number);
                    await Assert.That(result.Number).IsEqualTo(13);
                    await Assert.That(host.GetArray()[0]).IsEqualTo(2);
                    await Assert.That(host.GetArray()[1]).IsEqualTo(5);
                    await Assert.That(host.GetArray()[2]).IsEqualTo(6);
                }
            );
        }

        [global::TUnit.Core.Test]
        public Task RegCollGenIteratorOnMultiDimArrayChangeElem()
        {
            return RunScriptAsync(
                @"
                local array = o:GetMultiArray()
                array[0, 1] = array[1, 2];
                local x = 0;
                for i in array do
                    x = x + i;
                end
                return x;
                ",
                async (result, host) =>
                {
                    await Assert.That(result.Type).IsEqualTo(DataType.Number);
                    await Assert.That(result.Number).IsEqualTo(41);
                    await Assert.That(host.GetMultiArray()[0, 0]).IsEqualTo(2);
                    await Assert.That(host.GetMultiArray()[0, 1]).IsEqualTo(9);
                    await Assert.That(host.GetMultiArray()[0, 2]).IsEqualTo(6);
                    await Assert.That(host.GetMultiArray()[1, 0]).IsEqualTo(7);
                    await Assert.That(host.GetMultiArray()[1, 1]).IsEqualTo(8);
                    await Assert.That(host.GetMultiArray()[1, 2]).IsEqualTo(9);
                }
            );
        }

        [global::TUnit.Core.Test]
        public Task RegCollGenIteratorOnObjListAuto()
        {
            return RunScriptAsync(
                @"
                local list = o:GetItems()
                local x = 0;
                for i in list do
                    x = x + i.Value;
                end
                return x;
                ",
                async result =>
                {
                    await Assert.That(result.Type).IsEqualTo(DataType.Number);
                    await Assert.That(result.Number).IsEqualTo(24);
                }
            );
        }

        [global::TUnit.Core.Test]
        public Task RegCollGenIteratorOnObjListManual()
        {
            return RunScriptAsync(
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
                async result =>
                {
                    await Assert.That(result.Type).IsEqualTo(DataType.Number);
                    await Assert.That(result.Number).IsEqualTo(24);
                }
            );
        }

        [global::TUnit.Core.Test]
        public Task RegCollGenIteratorOnObjListChangeElem()
        {
            return RunScriptAsync(
                @"
                local list = o:GetItems()
                list[1] = ctor.__new(list[2].Value + list[1].Value);
                local x = 0;
                for i in list do
                    x = x + i.Value;
                end
                return x;
                ",
                async (result, host) =>
                {
                    await Assert.That(result.Type).IsEqualTo(DataType.Number);
                    await Assert.That(result.Number).IsEqualTo(7 + 17 + 9);
                    await Assert.That(host.GetItems()[0].Value).IsEqualTo(7);
                    await Assert.That(host.GetItems()[1].Value).IsEqualTo(17);
                    await Assert.That(host.GetItems()[2].Value).IsEqualTo(9);
                }
            );
        }

        private static Task RunScriptAsync(string code, Func<DynValue, Task> asserts)
        {
            return RunScriptAsync(code, async (value, _) => await asserts(value));
        }

        private static async Task RunScriptAsync(
            string code,
            Func<DynValue, RegCollMethods, Task> asserts
        )
        {
            List<int> sentinel = new() { 1, 1, 1, 1 };
            sentinel.Add(sentinel.Sum());
            sentinel.Add(sentinel.Last());

            try
            {
                UserData.RegisterType<RegCollMethods>();
                UserData.RegisterType<RegCollItem>();
                UserData.RegisterType<Array>();
                UserData.RegisterType(typeof(List<>));
                UserData.RegisterType<List<RegCollItem>>();
                UserData.RegisterType<List<int>>();
                UserData.RegisterExtensionType(typeof(Enumerable));

                Script script = new();
                RegCollMethods host = new();
                script.Globals["o"] = host;
                script.Globals["ctor"] = UserData.CreateStatic<RegCollItem>();

                DynValue result = script.DoString(code);
                await asserts(result, host);
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
                UserData.UnregisterType(typeof(List<>));
                UserData.UnregisterType<List<RegCollItem>>();
                UserData.UnregisterType<List<int>>();
            }
        }
    }
}
#pragma warning restore CA2007
#pragma warning restore CA1814
