namespace NovaSharp.Interpreter.Tests
{
    using NUnit.Framework;

    [TestFixture]
    public class TestMoreTests
    {
        [Test]
        public void TestMore000Sanity()
        {
            TapRunner.Run(@"TestMore/000-sanity.t");
        }

        [Test]
        public void TestMore001If()
        {
            TapRunner.Run(@"TestMore/001-if.t");
        }

        [Test]
        public void TestMore002Table()
        {
            TapRunner.Run(@"TestMore/002-table.t");
        }

        [Test]
        public void TestMore011While()
        {
            TapRunner.Run(@"TestMore/011-while.t");
        }

        [Test]
        public void TestMore012Repeat()
        {
            TapRunner.Run(@"TestMore/012-repeat.t");
        }

        [Test]
        public void TestMore014Fornum()
        {
            TapRunner.Run(@"TestMore/014-fornum.t");
        }

        [Test]
        public void TestMore015Forlist()
        {
            TapRunner.Run(@"TestMore/015-forlist.t");
        }

        [Test]
        //[Ignore]
        public void TestMore101Boolean()
        {
            TapRunner.Run(@"TestMore/101-boolean.t");
        }

        [Test]
        public void TestMore102Function()
        {
            TapRunner.Run(@"TestMore/102-function.t");
        }

        [Test]
        public void TestMore103Nil()
        {
            TapRunner.Run(@"TestMore/103-nil.t");
        }

        [Test]
        public void TestMore104Number()
        {
            TapRunner.Run(@"TestMore/104-number.t");
        }

        [Test]
        public void TestMore105String()
        {
            TapRunner.Run(@"TestMore/105-string.t");
        }

        [Test]
        public void TestMore106Table()
        {
            TapRunner.Run(@"TestMore/106-table.t");
        }

        [Test]
        public void TestMore107Thread()
        {
            TapRunner.Run(@"TestMore/107-thread.t");
        }

        //[Test]
        //[Ignore]
        // It's just a bunch of checks for error messages and nothing more useful. Userdata is tested by standard
        // end to end tests.
        //public void TestMore108Userdata()
        //{
        //	TapRunner.Run(@"TestMore/108-userdata.t");
        //}

        [Test]
        public void TestMore200Examples()
        {
            TapRunner.Run(@"TestMore/200-examples.t");
        }

        [Test]
        public void TestMore201Assign()
        {
            TapRunner.Run(@"TestMore/201-assign.t");
        }

        [Test]
        public void TestMore202Expr()
        {
            TapRunner.Run(@"TestMore/202-expr.t");
        }

        [Test]
        public void TestMore203Lexico()
        {
            TapRunner.Run(@"TestMore/203-lexico.t");
        }

        [Test]
        public void TestMore204Grammar()
        {
            TapRunner.Run(@"TestMore/204-grammar.t");
        }

        [Test]
        public void TestMore211Scope()
        {
            TapRunner.Run(@"TestMore/211-scope.t");
        }

        [Test]
        public void TestMore212Function()
        {
            TapRunner.Run(@"TestMore/212-function.t");
        }

        [Test]
        public void TestMore213Closure()
        {
            TapRunner.Run(@"TestMore/213-closure.t");
        }

        [Test]
        public void TestMore214Coroutine()
        {
            TapRunner.Run(@"TestMore/214-coroutine.t");
        }

        [Test]
        public void TestMore221Table()
        {
            TapRunner.Run(@"TestMore/221-table.t");
        }

        [Test]
        public void TestMore222Constructor()
        {
            TapRunner.Run(@"TestMore/222-constructor.t");
        }

        [Test]
        public void TestMore223Iterator()
        {
            TapRunner.Run(@"TestMore/223-iterator.t");
        }

        [Test]
        public void TestMore231Metatable()
        {
            TapRunner.Run(@"TestMore/231-metatable.t");
        }

        [Test]
        public void TestMore232Object()
        {
            TapRunner.Run(@"TestMore/232-object.t");
        }

        [Test]
        public void TestMore301Basic()
        {
            TapRunner.Run(@"TestMore/301-basic.t");
        }

        [Test]
        public void TestMore304String()
        {
            TapRunner.Run(@"TestMore/304-string.t");
        }

        [Test]
        public void TestMore305Table()
        {
            TapRunner.Run(@"TestMore/305-table.t");
        }

        [Test]
        public void TestMore306Math()
        {
            TapRunner.Run(@"TestMore/306-math.t");
        }

        [Test]
        public void TestMore307Bit()
        {
            TapRunner.Run(@"TestMore/307-bit.t");
        }

        private bool AreCoreModulesFullySupported(CoreModules modules)
        {
            CoreModules supp = Script.GlobalOptions.Platform.FilterSupportedCoreModules(modules);
            return supp == modules;
        }

        //[Test]
        //[Ignore]
        //public void TestMore310Debug()
        //{
        //	TapRunner.Run(@"TestMore/310-debug.t");
        //}

        [Test]
        public void TestMore314Regex()
        {
            TapRunner.Run(@"TestMore/314-regex.t");
        }

        //[Test]
        //[Ignore]
        //public void TestMore320Stdin()
        //{
        //	TapRunner.Run(@"TestMore/310-stdin.t");
        //}
    }
}
