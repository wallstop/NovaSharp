namespace NovaSharp.Interpreter.Tests.Units
{
    using NovaSharp.Interpreter;
    using NovaSharp.Interpreter.DataTypes;
    using NUnit.Framework;

    [TestFixture]
    public class TailCallTests
    {
        [Test]
        public void TailRecursionHandlesThousandsOfFrames()
        {
            Script script = new();
            DynValue result = script.DoString(
                @"
                local function accumulate(n, acc)
                    if n == 0 then
                        return acc
                    end
                    return accumulate(n - 1, acc + 1)
                end

                return accumulate(20000, 0)
            "
            );

            Assert.Multiple(() =>
            {
                Assert.That(result.Type, Is.EqualTo(DataType.Number));
                Assert.That(result.Number, Is.EqualTo(20000));
            });
        }

        [Test]
        public void TailCallPreservesMultipleReturnValues()
        {
            Script script = new();
            DynValue result = script.DoString(
                @"
                local function id(a, b, c)
                    return a, b, c
                end

                local function forward(...)
                    return id(...)
                end

                return forward(1, 2, 3)
            "
            );

            Assert.Multiple(() =>
            {
                Assert.That(result.Type, Is.EqualTo(DataType.Tuple));
                Assert.That(result.Tuple.Length, Is.EqualTo(3));
                Assert.That(result.Tuple[0].Number, Is.EqualTo(1));
                Assert.That(result.Tuple[1].Number, Is.EqualTo(2));
                Assert.That(result.Tuple[2].Number, Is.EqualTo(3));
            });
        }
    }
}
