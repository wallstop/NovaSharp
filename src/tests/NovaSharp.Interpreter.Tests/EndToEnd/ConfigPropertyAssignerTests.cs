namespace NovaSharp.Interpreter.Tests.EndToEnd
{
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NovaSharp.Interpreter.Errors;
    using NovaSharp.Interpreter.Interop;
    using NovaSharp.Interpreter.Interop.Attributes;
    using NovaSharp.Interpreter.Modules;
    using NUnit.Framework;

    [TestFixture]
    public class ConfigPropertyAssignerTests
    {
        private sealed class MySubclass
        {
            [NovaSharpProperty]
            public string MyString { get; set; }

            [NovaSharpProperty("number")]
            public int MyNumber { get; private set; }
        }

        private sealed class MyClass
        {
            [NovaSharpProperty]
            public string MyString { get; set; }

            [NovaSharpProperty("number")]
            public int MyNumber { get; private set; }

            [NovaSharpProperty]
            internal Table SomeTable { get; private set; }

            [NovaSharpProperty]
            public DynValue NativeValue { get; private set; }

            [NovaSharpProperty]
            public MySubclass SubObj { get; private set; }
        }

        private static MyClass Test(string tableDef)
        {
            _ = new MySubclass();

            Script s = new(default(CoreModules));

            DynValue table = s.DoString("return " + tableDef);

            Assert.That(table.Type, Is.EqualTo(DataType.Table));

            PropertyTableAssigner<MyClass> pta = new("class");
            PropertyTableAssigner<MySubclass> pta2 = new();

            pta.SetSubassigner(pta2);

            MyClass o = new();

            pta.AssignObject(o, table.Table);

            return o;
        }

        [Test]
        public void ConfigPropSimpleAssign()
        {
            MyClass x = Test(
                @"
				{
				class = 'oohoh',
				myString = 'ciao',
				number = 3,
				some_table = {},
				nativeValue = function() end,
				subObj = { number = 15, myString = 'hi' },
				}"
            );

            Assert.That(3, Is.EqualTo(x.MyNumber));
            Assert.That("ciao", Is.EqualTo(x.MyString));
            Assert.That(DataType.Function, Is.EqualTo(x.NativeValue.Type));
            Assert.That(15, Is.EqualTo(x.SubObj.MyNumber));
            Assert.That("hi", Is.EqualTo(x.SubObj.MyString));
            Assert.That(x.SomeTable, Is.Not.Null);
        }

        [Test]
        [ExpectedException(typeof(ScriptRuntimeException))]
        public void ConfigPropThrowsOnInvalid()
        {
            MyClass x = Test(
                @"
				{
				class = 'oohoh',
				myString = 'ciao',
				number = 3,
				some_table = {},
				invalid = 3,
				nativeValue = function() end,
				}"
            );

            Assert.That(3, Is.EqualTo(x.MyNumber));
            Assert.That("ciao", Is.EqualTo(x.MyString));
            Assert.That(DataType.Function, Is.EqualTo(x.NativeValue.Type));
            Assert.That(x.SomeTable, Is.Not.Null);
        }
    }
}
