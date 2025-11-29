#pragma warning disable CA2007
namespace NovaSharp.Interpreter.Tests.TUnit.EndToEnd
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using global::TUnit.Assertions;
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Interop;
    using NovaSharp.Interpreter.Tests;

    [UserDataIsolation]
    public sealed class UserDataIndexerTUnitTests
    {
        internal sealed class IndexerTestClass
        {
            private readonly Dictionary<int, int> _mymap = new();

            public int this[int idx]
            {
                get => _mymap[idx];
                set => _mymap[idx] = value;
            }

            public int this[int idx1, int idx2, int idx3]
            {
                get
                {
                    int idx = (idx1 + idx2) * idx3;
                    return _mymap[idx];
                }
                set
                {
                    int idx = (idx1 + idx2) * idx3;
                    _mymap[idx] = value;
                }
            }
        }

        private static Task RunIndexerTestAsync(string code, int expected)
        {
            Script script = new();
            IndexerTestClass obj = new();

            UserData.RegisterType<IndexerTestClass>();

            script.Globals.Set("o", UserData.Create(obj));

            DynValue result = script.DoString(code);

            return VerifyAsync(result);

            async Task VerifyAsync(DynValue value)
            {
                await Assert.That(value.Type).IsEqualTo(DataType.Number);
                await Assert.That(value.Number).IsEqualTo(expected);
            }
        }

        [global::TUnit.Core.Test]
        public Task InteropSingleSetterOnly()
        {
            return RunIndexerTestAsync("o[1] = 1; return 13", 13);
        }

        [global::TUnit.Core.Test]
        public Task InteropSingleIndexerGetSet()
        {
            return RunIndexerTestAsync("o[5] = 19; return o[5];", 19);
        }

        [global::TUnit.Core.Test]
        public Task InteropMultiIndexerGetSet()
        {
            return RunIndexerTestAsync("o[1,2,3] = 47; return o[1,2,3];", 47);
        }

        [global::TUnit.Core.Test]
        public Task InteropMultiIndexerMetatableGetSet()
        {
            string script =
                @"
                m = {
                    __index = o,
                    __newindex = o
                }

                t = { }

                setmetatable(t, m);

                t[10,11,12] = 1234; return t[10,11,12];";

            return RunIndexerTestAsync(script, 1234);
        }

        [global::TUnit.Core.Test]
        public async Task InteropMultiIndexerMetamethodGetSet()
        {
            string script =
                @"
                m = {
                    __index = function() end,
                    __newindex = function() end
                }

                t = { }

                setmetatable(t, m);

                t[10,11,12] = 1234; return t[10,11,12];";

            ScriptRuntimeException exception = Assert.Throws<ScriptRuntimeException>(() =>
                RunIndexerTestAsync(script, 1234).GetAwaiter().GetResult()
            );

            await Assert.That(exception).IsNotNull();
        }

        [global::TUnit.Core.Test]
        public Task InteropMixedIndexerGetSet()
        {
            return RunIndexerTestAsync("o[3,2,3] = 119; return o[15];", 119);
        }

        [global::TUnit.Core.Test]
        public Task InteropExpListIndexingCompilesButNotRun1()
        {
            string script =
                @"
                x = { 99, 98, 97, 96 }
                return x[2,3];
                ";

            Assert.Throws<ScriptRuntimeException>(() => Script.RunString(script));
            return Task.CompletedTask;
        }

        [global::TUnit.Core.Test]
        public Task InteropExpListIndexingCompilesButNotRun2()
        {
            string script =
                @"
                x = { 99, 98, 97, 96 }
                x[2,3] = 5;
                ";

            Assert.Throws<ScriptRuntimeException>(() => Script.RunString(script));
            return Task.CompletedTask;
        }
    }
}
#pragma warning restore CA2007
