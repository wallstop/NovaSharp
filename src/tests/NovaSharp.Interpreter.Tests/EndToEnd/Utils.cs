namespace NovaSharp.Interpreter.Tests.EndToEnd
{
    using NovaSharp.Interpreter.DataTypes;
    using NUnit.Framework;

    public static class Utils
    {
        public static void DynAssert(DynValue result, params object[] args)
        {
            if (args == null)
            {
                args = new object[1] { DataType.Void };
            }

            if (args.Length == 1)
            {
                DynAssertValue(args[0], result);
            }
            else
            {
                Assert.That(result.Type, Is.EqualTo(DataType.Tuple));
                Assert.That(result.Tuple.Length, Is.EqualTo(args.Length));

                for (int i = 0; i < args.Length; i++)
                {
                    DynAssertValue(args[i], result.Tuple[i]);
                }
            }
        }

        private static void DynAssertValue(object reference, DynValue dynValue)
        {
            if (reference == (object)DataType.Void)
            {
                Assert.That(dynValue.Type, Is.EqualTo(DataType.Void));
            }
            else if (reference == null)
            {
                Assert.That(dynValue.Type, Is.EqualTo(DataType.Nil));
            }
            else if (reference is double d)
            {
                Assert.That(dynValue.Type, Is.EqualTo(DataType.Number));
                Assert.That(dynValue.Number, Is.EqualTo(d));
            }
            else if (reference is int i)
            {
                Assert.That(dynValue.Type, Is.EqualTo(DataType.Number));
                Assert.That(dynValue.Number, Is.EqualTo(i));
            }
            else if (reference is string s)
            {
                Assert.That(dynValue.Type, Is.EqualTo(DataType.String));
                Assert.That(dynValue.String, Is.EqualTo(s));
            }
        }
    }
}
