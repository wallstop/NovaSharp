namespace NovaSharp.Interpreter.Tests.TUnit.Tap
{
    using System.Threading.Tasks;
    using global::TUnit.Core;
    using NovaSharp.Interpreter.Compatibility;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Tests;

    [UserDataIsolation]
    public sealed class TestMoreTUnitTests
    {
        private static Task RunTapAsync(
            string relativePath,
            LuaCompatibilityVersion? compatibilityVersion = null
        )
        {
            using (UserData.BeginIsolationScope())
            {
                TapRunnerTUnit.Run(relativePath, compatibilityVersion);
            }
            return Task.CompletedTask;
        }

        [Test]
        public Task TestMore000Sanity()
        {
            return RunTapAsync("TestMore/000-sanity.t");
        }

        [Test]
        public Task TestMore001If()
        {
            return RunTapAsync("TestMore/001-if.t");
        }

        [Test]
        public Task TestMore002Table()
        {
            return RunTapAsync("TestMore/002-table.t");
        }

        [Test]
        public Task TestMore011While()
        {
            return RunTapAsync("TestMore/011-while.t");
        }

        [Test]
        public Task TestMore012Repeat()
        {
            return RunTapAsync("TestMore/012-repeat.t");
        }

        [Test]
        public Task TestMore014Fornum()
        {
            return RunTapAsync("TestMore/014-fornum.t");
        }

        [Test]
        public Task TestMore015Forlist()
        {
            return RunTapAsync("TestMore/015-forlist.t");
        }

        [Test]
        public Task TestMore101Boolean()
        {
            return RunTapAsync("TestMore/101-boolean.t");
        }

        [Test]
        public Task TestMore102Function()
        {
            return RunTapAsync("TestMore/102-function.t");
        }

        [Test]
        public Task TestMore103Nil()
        {
            return RunTapAsync("TestMore/103-nil.t");
        }

        [Test]
        public Task TestMore104Number()
        {
            return RunTapAsync("TestMore/104-number.t");
        }

        [Test]
        public Task TestMore105String()
        {
            return RunTapAsync("TestMore/105-string.t");
        }

        [Test]
        public Task TestMore106Table()
        {
            return RunTapAsync("TestMore/106-table.t");
        }

        [Test]
        public Task TestMore107Thread()
        {
            return RunTapAsync("TestMore/107-thread.t");
        }

        [Test]
        public Task TestMore200Examples()
        {
            return RunTapAsync("TestMore/200-examples.t");
        }

        [Test]
        public Task TestMore201Assign()
        {
            return RunTapAsync("TestMore/201-assign.t");
        }

        [Test]
        public Task TestMore202Expr()
        {
            return RunTapAsync("TestMore/202-expr.t");
        }

        [Test]
        public Task TestMore203Lexico()
        {
            return RunTapAsync("TestMore/203-lexico.t");
        }

        [Test]
        public Task TestMore204Grammar()
        {
            return RunTapAsync("TestMore/204-grammar.t");
        }

        [Test]
        public Task TestMore211Scope()
        {
            return RunTapAsync("TestMore/211-scope.t");
        }

        [Test]
        public Task TestMore212Function()
        {
            return RunTapAsync("TestMore/212-function.t");
        }

        [Test]
        public Task TestMore213Closure()
        {
            return RunTapAsync("TestMore/213-closure.t");
        }

        [Test]
        public Task TestMore214Coroutine()
        {
            return RunTapAsync("TestMore/214-coroutine.t");
        }

        [Test]
        public Task TestMore221Table()
        {
            return RunTapAsync("TestMore/221-table.t");
        }

        [Test]
        public Task TestMore222Constructor()
        {
            return RunTapAsync("TestMore/222-constructor.t");
        }

        [Test]
        public Task TestMore223Iterator()
        {
            return RunTapAsync("TestMore/223-iterator.t");
        }

        [Test]
        public Task TestMore231Metatable()
        {
            return RunTapAsync("TestMore/231-metatable.t");
        }

        [Test]
        public Task TestMore232Object()
        {
            return RunTapAsync("TestMore/232-object.t");
        }

        [Test]
        public Task TestMore301Basic()
        {
            return RunTapAsync("TestMore/301-basic.t");
        }

        [Test]
        public Task TestMore304String()
        {
            return RunTapAsync("TestMore/304-string.t");
        }

        [Test]
        public Task TestMore305Table()
        {
            return RunTapAsync("TestMore/305-table.t");
        }

        [Test]
        public Task TestMore306Math()
        {
            return RunTapAsync("TestMore/306-math.t");
        }

        [Test]
        public Task TestMore307Bit()
        {
            return RunTapAsync("TestMore/307-bit.t", LuaCompatibilityVersion.Lua52);
        }

        [Test]
        public Task TestMore310CloseVar()
        {
            return RunTapAsync("TestMore/310-close-var.t");
        }

        [Test]
        public Task TestMore314Regex()
        {
            return RunTapAsync("TestMore/314-regex.t");
        }
    }
}
