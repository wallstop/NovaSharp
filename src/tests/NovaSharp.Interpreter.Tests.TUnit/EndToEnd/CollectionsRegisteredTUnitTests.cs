#pragma warning disable CA1814
namespace NovaSharp.Interpreter.Tests.TUnit.EndToEnd
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Interop;
    using NovaSharp.Tests.TestInfrastructure.Scopes;

    [UserDataIsolation]
    public sealed class CollectionsRegisteredTUnitTests
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

            public List<int>.Enumerator GetEnumerator() => _list.GetEnumerator();

            [SuppressMessage(
                "Design",
                "CA1024:UsePropertiesWhereAppropriate",
                Justification = "Lua interop coverage requires method syntax."
            )]
            public List<int> GetList()
            {
                return _list;
            }

            [SuppressMessage("Design", "CA1024", Justification = "Interop coverage")]
            public int[] GetArray()
            {
                return _array;
            }

            [SuppressMessage("Design", "CA1024", Justification = "Interop coverage")]
            public int[,] GetMultiArray()
            {
                return _multiArray;
            }

            [SuppressMessage("Design", "CA1024", Justification = "Interop coverage")]
            public List<RegCollItem> GetItems()
            {
                return _items;
            }
        }

        [global::TUnit.Core.Test]
        public Task RegCollIteratorOnListAuto()
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
                    await EndToEndDynValueAssert.ExpectAsync(result, 6).ConfigureAwait(false)
            );
        }

        [global::TUnit.Core.Test]
        public Task RegCollIteratorOnListManual()
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
                    await EndToEndDynValueAssert.ExpectAsync(result, 6).ConfigureAwait(false)
            );
        }

        [global::TUnit.Core.Test]
        public Task RegCollIteratorOnListChangeElem()
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
                    await EndToEndDynValueAssert.ExpectAsync(result, 9).ConfigureAwait(false);
                    await Assert.That(host.GetList()[0]).IsEqualTo(1).ConfigureAwait(false);
                    await Assert.That(host.GetList()[1]).IsEqualTo(5).ConfigureAwait(false);
                    await Assert.That(host.GetList()[2]).IsEqualTo(3).ConfigureAwait(false);
                }
            );
        }

        [global::TUnit.Core.Test]
        public Task RegCollIteratorOnArrayAuto()
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
                    await EndToEndDynValueAssert.ExpectAsync(result, 12).ConfigureAwait(false)
            );
        }

        [global::TUnit.Core.Test]
        public Task RegCollIteratorOnArrayChangeElem()
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
                    await EndToEndDynValueAssert.ExpectAsync(result, 13).ConfigureAwait(false);
                    await Assert.That(host.GetArray()[0]).IsEqualTo(2).ConfigureAwait(false);
                    await Assert.That(host.GetArray()[1]).IsEqualTo(5).ConfigureAwait(false);
                    await Assert.That(host.GetArray()[2]).IsEqualTo(6).ConfigureAwait(false);
                }
            );
        }

        [global::TUnit.Core.Test]
        public Task RegCollIteratorOnMultiDimArrayChangeElem()
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
                    await EndToEndDynValueAssert.ExpectAsync(result, 41).ConfigureAwait(false);
                    await Assert
                        .That(host.GetMultiArray()[0, 1])
                        .IsEqualTo(9)
                        .ConfigureAwait(false);
                }
            );
        }

        [global::TUnit.Core.Test]
        public Task RegCollIteratorOnObjListAuto()
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
                    await EndToEndDynValueAssert.ExpectAsync(result, 24).ConfigureAwait(false)
            );
        }

        [global::TUnit.Core.Test]
        public Task RegCollIteratorOnObjListManual()
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
                    await EndToEndDynValueAssert.ExpectAsync(result, 24).ConfigureAwait(false)
            );
        }

        [global::TUnit.Core.Test]
        public Task RegCollIteratorOnObjListChangeElem()
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
                    await EndToEndDynValueAssert
                        .ExpectAsync(result, 7 + 17 + 9)
                        .ConfigureAwait(false);
                    await Assert.That(host.GetItems()[1].Value).IsEqualTo(17).ConfigureAwait(false);
                }
            );
        }

        private static Task RunScriptAsync(string code, Func<DynValue, Task> asserts)
        {
            return RunScriptAsync(
                code,
                async (value, _) => await asserts(value).ConfigureAwait(false)
            );
        }

        private static Task RunScriptAsync(
            string code,
            Func<DynValue, RegCollMethods, Task> asserts
        )
        {
            using UserDataRegistrationScope registrationScope = UserDataRegistrationScope.Track(
                ensureUnregistered: true,
                typeof(RegCollMethods),
                typeof(RegCollItem),
                typeof(List<RegCollItem>),
                typeof(List<int>),
                typeof(int[]),
                typeof(int[,])
            );

            try
            {
                registrationScope.RegisterType<RegCollMethods>();
                registrationScope.RegisterType<RegCollItem>();
                registrationScope.RegisterType<List<RegCollItem>>();
                registrationScope.RegisterType<List<int>>();
                registrationScope.RegisterType<int[]>();
                registrationScope.RegisterType<int[,]>();

                Script script = new();
                RegCollMethods host = new();
                script.Globals["o"] = host;
                script.Globals["ctor"] = UserData.CreateStatic<RegCollItem>();

                DynValue result = script.DoString(code);
                return asserts(result, host);
            }
            catch (ScriptRuntimeException ex)
            {
                Debug.WriteLine(ex.DecoratedMessage);
                ex.Rethrow();
                throw;
            }
        }
    }
}
